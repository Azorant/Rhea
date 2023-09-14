using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace Rhea.Services;

internal sealed class DiscordClientHost : IHostedService
{
    private readonly DiscordSocketClient client;
    private readonly InteractionService interactionService;
    private readonly IServiceProvider serviceProvider;

    public DiscordClientHost(
        DiscordSocketClient client,
        InteractionService interactionService,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        this.client = client;
        this.interactionService = interactionService;
        this.serviceProvider = serviceProvider;
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
    }

    private async Task JoinedGuild(SocketGuild guild)
    {
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

    private static async Task SlashCommandExecuted(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
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

    public static string DisplayName(IUser user) => user.Discriminator == "0000"
        ? user.Username
        : $"{user.Username}#{user.Discriminator}";

    private static string ParseArgs(IEnumerable<SocketSlashCommandDataOption> data)
    {
        List<string> args = new();

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