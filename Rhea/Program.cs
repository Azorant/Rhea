using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Artwork;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Rhea;

public class Program
{
    private readonly IServiceProvider serviceProvider;

    private Program()
    {
        serviceProvider = CreateProvider();
    }

    private static void Main()
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
            new Program().RunAsync().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            Log.Error(error, "Error from main");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IServiceProvider CreateProvider()
    {
        var collection = new ServiceCollection()
            .AddSingleton(new LoggerFactory())
            .AddSingleton(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMembers | GatewayIntents.Guilds
            })
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(new InteractionServiceConfig())
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<IDiscordClientWrapper, DiscordClientWrapper>()
            .AddSingleton<IAudioService, LavalinkNode>()
            .AddSingleton(new LavalinkNodeOptions
            {
                RestUri = $"http://{Environment.GetEnvironmentVariable("LAVALINK_HOST")}/",
                WebSocketUri = $"ws://{Environment.GetEnvironmentVariable("LAVALINK_HOST")}/",
                Password = Environment.GetEnvironmentVariable("LAVALINK_AUTH")!,
                ResumeKey = "Rhea"
            })
            .AddSingleton<IArtworkService, ArtworkService>()
            .AddSingleton<InactivityTrackingOptions>()
            .AddSingleton<InactivityTrackingService>();

        return collection.BuildServiceProvider();
    }

    private async Task RunAsync()
    {
        var client = serviceProvider.GetRequiredService<DiscordSocketClient>();
        var handler = serviceProvider.GetRequiredService<InteractionService>();
        var lavalink = serviceProvider.GetRequiredService<IAudioService>();

        await handler.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

        client.Log += LogAsync;
        handler.Log += LogAsync;

        client.Ready += async () =>
        {
            await lavalink.InitializeAsync();
            Log.Information("[Lavalink] Connected");
            serviceProvider.GetRequiredService<InactivityTrackingService>()
                .BeginTracking();
            if (IsDebug())
                await handler.RegisterCommandsToGuildAsync(ulong.Parse(Environment.GetEnvironmentVariable("DEV_GUILD")!));
            else
                await handler.RegisterCommandsGloballyAsync();
        };

        client.InteractionCreated += async interaction =>
        {
            try
            {
                var context = new SocketInteractionContext(client, interaction);

                await handler.ExecuteCommandAsync(context, serviceProvider);
            }
            catch
            {
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        };

        handler.SlashCommandExecuted += SlashCommandExecuted;

        await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("TOKEN"));
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite);
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
                    embed.Title = "Error Occurred";
                    embed.Description = "I ran into a problem running your command.\nIf it continues to happen join the support server";
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
                $"[Command] {guild} {context.User.Username}#{context.User.Discriminator} ({context.User.Id}) ran /{(string.IsNullOrEmpty(command.Module.Parent?.SlashGroupName) ? string.Empty : command.Module.Parent.SlashGroupName + ' ')}{(string.IsNullOrEmpty(command.Module.SlashGroupName) ? string.Empty : command.Module.SlashGroupName + ' ')}{command.Name} {ParseArgs(((SocketSlashCommandData)context.Interaction.Data).Options)}");

        }
    }

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

    private static bool IsDebug()
    {
#if DEBUG
        return true;
#else
            return false;
#endif
    }
}