using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Cluster;
using Lavalink4NET.Cluster.Events;
using Lavalink4NET.Cluster.Nodes;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Protocol.Models;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Hosting;
using Rhea.Bot.Models;
using Serilog;
using Serilog.Events;

namespace Rhea.Bot.Services;

internal sealed class DiscordClientHost : IHostedService
{
    private readonly DiscordSocketClient client;
    private readonly InteractionService interactionService;
    private readonly IAudioService lavalink;
    private readonly IServiceProvider serviceProvider;
    private readonly Statistics stats;

    public DiscordClientHost(
        DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider serviceProvider, IAudioService lavalink, Statistics stats)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.client = client;
        this.interactionService = interactionService;
        this.serviceProvider = serviceProvider;
        this.lavalink = lavalink;
        this.stats = stats;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        client.InteractionCreated += InteractionCreated;
        client.Ready += ClientReady;
        client.JoinedGuild += JoinedGuild;
        client.LeftGuild += LeftGuild;
        client.Log += LogAsync;
        interactionService.Log += LogAsync;
        interactionService.SlashCommandExecuted += SlashCommandExecuted;
        lavalink.TrackStarted += TrackStarted;
        lavalink.TrackException += TrackException;
        ((ClusterAudioService)lavalink).NodeStatusChanged += NodeStatusChanged;

        await client
            .LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"))
            .ConfigureAwait(false);

        await client
            .StartAsync()
            .ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        client.InteractionCreated -= InteractionCreated;
        client.Ready -= ClientReady;
        client.JoinedGuild -= JoinedGuild;
        client.LeftGuild -= LeftGuild;
        client.Log -= LogAsync;
        interactionService.Log -= LogAsync;
        interactionService.SlashCommandExecuted -= SlashCommandExecuted;
        lavalink.TrackStarted -= TrackStarted;
        lavalink.TrackException -= TrackException;
        ((ClusterAudioService)lavalink).NodeStatusChanged -= NodeStatusChanged;

        await client
            .StopAsync()
            .ConfigureAwait(false);
    }

    private async Task InteractionCreated(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(client, interaction);

            await interactionService.ExecuteCommandAsync(context, serviceProvider);
        }
        catch
        {
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
        }
    }

    private async Task ClientReady()
    {
        await interactionService
            .AddModulesAsync(Assembly.GetExecutingAssembly(), serviceProvider)
            .ConfigureAwait(false);

        if (IsDebug())
            await interactionService.RegisterCommandsToGuildAsync(ulong.Parse(Environment.GetEnvironmentVariable("DEV_GUILD")!));
        else
            await interactionService.RegisterCommandsGloballyAsync();
        stats.Guilds?.Set(client.Guilds.Count);
    }

    private async Task JoinedGuild(SocketGuild guild)
    {
        stats.Guilds?.Inc();
        if (!ulong.TryParse(Environment.GetEnvironmentVariable("GUILD_CHANNEL"), out var channelID)) return;

        if (client.GetChannel(channelID) is not SocketTextChannel channel || channel.GetChannelType() != ChannelType.Text) return;

        var user = await client.GetUserAsync(guild.OwnerId);
        var owner = user == null
            ? $"**Owner ID:** {guild.OwnerId}"
            : $"**Owner:** {DisplayName(user)}\n**Owner ID:** {user.Id}";

        await channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle("Joined guild")
            .WithDescription($"**Name:** {guild.Name}\n**ID:** {guild.Id}\n{owner}\n**Members:** {guild.MemberCount}\n**Created:** {guild.CreatedAt:f}")
            .WithColor(Color.Green)
            .WithCurrentTimestamp()
            .WithThumbnailUrl(guild.IconUrl).Build());
    }

    private async Task LeftGuild(SocketGuild guild)
    {
        stats.Guilds?.Dec();
        if (!ulong.TryParse(Environment.GetEnvironmentVariable("GUILD_CHANNEL"), out var channelID)) return;

        if (client.GetChannel(channelID) is not SocketTextChannel channel || channel.GetChannelType() != ChannelType.Text) return;

        var user = await client.GetUserAsync(guild.OwnerId);
        var owner = user == null
            ? $"**Owner ID:** {guild.OwnerId}"
            : $"**Owner:** {DisplayName(user)}\n**Owner ID:** {user.Id}";

        await channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle("Left guild")
            .WithDescription($"**Name:** {guild.Name}\n**ID:** {guild.Id}\n{owner}\n**Members:** {guild.MemberCount}\n**Created:** {guild.CreatedAt:f}")
            .WithColor(Color.Red)
            .WithCurrentTimestamp()
            .WithThumbnailUrl(guild.IconUrl).Build());
    }

    private static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }

    private async Task SlashCommandExecuted(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
        stats.Commands?.WithLabels([command.Name]).Inc();
        if (!result.IsSuccess)
        {
            Log.Warning("[Command] {ContextUser} tried to run {CommandName} but ran into {S}", context.User, command.Name, result.Error.ToString());
            var embed = new EmbedBuilder
            {
                Color = new Color(0x2F3136)
            };
            switch (result.Error)
            {
                case InteractionCommandError.BadArgs:
                    embed.Title = "Invalid Arguments";
                    embed.Description =
                        "Please make sure the arguments you're providing are correct.\nIf you keep running into this message, please join the support server";
                    break;
                case InteractionCommandError.ConvertFailed:
                case InteractionCommandError.Exception:
                    if (result.ErrorReason.StartsWith("Unable to connect to"))
                    {
                        embed.Title = "Missing Permissions";
                        embed.Description = result.ErrorReason;
                    }
                    else
                    {
                        embed.Title = "Something Happened";
                        embed.Description = "I was unable to run your command.\nIf it continues to happen join the support server";
                    }

                    break;
                case InteractionCommandError.UnmetPrecondition:
                    embed.Title = "Missing Permissions";
                    embed.Description = result.ErrorReason;
                    break;
                default:
                    embed.Title = "Something Happened";
                    embed.Description = "I was unable to run your command.\nIf it continues to happen join the support server";
                    break;
            }

            if (context.Interaction.HasResponded)
                await context.Interaction.ModifyOriginalResponseAsync(m => m.Embed = embed.Build());
            else
                await context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
        }
        else
        {
            var guild = context.Interaction.GuildId == null
                ? "DM"
                : $"{context.Guild.Name} ({context.Guild.Id}) #{context.Channel.Name} ({context.Channel.Id})";
            Log.Information(
                $"[Command] {guild} {DisplayName(context.User)} ({context.User.Id}) ran /{(string.IsNullOrEmpty(command.Module.Parent?.SlashGroupName) ? string.Empty : command.Module.Parent.SlashGroupName + ' ')}{(string.IsNullOrEmpty(command.Module.SlashGroupName) ? string.Empty : command.Module.SlashGroupName + ' ')}{command.Name} {ParseArgs(((SocketSlashCommandData)context.Interaction.Data).Options)}");
        }
    }

    private Task TrackStarted(object _, TrackStartedEventArgs args)
    {
        stats.TracksPlayed?.Inc();
        return Task.CompletedTask;
    }

    private async Task TrackException(object _, TrackExceptionEventArgs args)
    {
        var id = Guid.NewGuid();
        await LogAsync(new LogMessage(LogSeverity.Warning, "Lavalink", $"Track exception {id} for guild {args.Player.GuildId}: {args.Exception.Message}"));
        await ((CustomPlayer)args.Player).TextChannel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle("Track Exception")
            .AddField("Track Information", $"[{args.Track.Title}]({args.Track.Uri})")
            .AddField("Exception Encountered", args.Exception.Message, true)
            .WithFooter(id.ToString())
            .WithColor(Color.Gold).Build());

        if (!ulong.TryParse(Environment.GetEnvironmentVariable("LOG_CHANNEL"), out var channelID)) return;
        if (client.GetChannel(channelID) is not SocketTextChannel channel || channel.GetChannelType() != ChannelType.Text) return;
        await channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle($"{Enum.GetName(typeof(ExceptionSeverity), args.Exception.Severity)} Track Exception")
            .WithColor(Color.Orange)
            .WithCurrentTimestamp()
            .AddField("Exception", args.Exception.Message, true)
            .AddField("Node", args.Player.Label.Split("on").Last(), true)
            .AddField("Guild", args.Player.GuildId, true)
            .AddField("Track", $"[{args.Track.Title}]({args.Track.Uri})")
            .WithFooter(id.ToString())
            .Build());
    }

    private async Task NodeStatusChanged(object _, NodeStatusChangedEventArgs args)
    {
        await LogAsync(new LogMessage(LogSeverity.Warning, "Lavalink",
            $"{args.Node.Label} node status changed from {Enum.GetName(typeof(LavalinkNodeStatus), args.PreviousStatus)} to {Enum.GetName(typeof(LavalinkNodeStatus), args.CurrentStatus)}"));

        if (!ulong.TryParse(Environment.GetEnvironmentVariable("LOG_CHANNEL"), out var channelID)) return;
        if (client.GetChannel(channelID) is not SocketTextChannel channel || channel.GetChannelType() != ChannelType.Text) return;
        await channel.SendMessageAsync(embed: new EmbedBuilder()
            .WithTitle($"Node Status Changed")
            .WithColor(Color.Gold)
            .WithCurrentTimestamp()
            .AddField("Node", args.Node.Label, true)
            .AddField("Previous", Enum.GetName(typeof(LavalinkNodeStatus), args.PreviousStatus), true)
            .AddField("Current", Enum.GetName(typeof(LavalinkNodeStatus), args.CurrentStatus), true)
            .Build());
    }

    public static string DisplayName(IUser user) => user.Discriminator == "0000"
        ? user.Username
        : $"{user.Username}#{user.Discriminator}";

    private static string ParseArgs(IEnumerable<SocketSlashCommandDataOption> data)
    {
        var args = new List<string>();

        foreach (var option in data)
        {
            switch (option.Type)
            {
                case ApplicationCommandOptionType.SubCommand:
                case ApplicationCommandOptionType.SubCommandGroup:
                    args.Add(ParseArgs(option.Options));
                    break;
                case ApplicationCommandOptionType.Channel:
                case ApplicationCommandOptionType.Role:
                case ApplicationCommandOptionType.User:
                    args.Add($"{option.Name}:{((ISnowflakeEntity)option.Value).Id.ToString()}");
                    break;
                default:
                    args.Add($"{option.Name}:{option.Value}");
                    break;
            }
        }

        return string.Join(' ', args);
    }

    public static bool IsDebug()
    {
#if DEBUG
        return true;
#else
            return false;
#endif
    }
}