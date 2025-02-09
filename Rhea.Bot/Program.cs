using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rhea.Bot.Services;
using Serilog;
using Monitor = Rhea.Bot.Services.Monitor;

namespace Rhea.Bot;

using Monitor = Services.Monitor;

public class Program
{
    private Program()
    {
        var builder = new HostApplicationBuilder();
        BuildServices(builder.Services);
        builder.Build().Run();
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
            _ = new Program();
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

    private static void BuildServices(IServiceCollection services)
    {
        services
            .AddSingleton(new LoggerFactory())
            .AddSingleton(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.GuildVoiceStates | GatewayIntents.GuildMembers | GatewayIntents.Guilds
            })
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<IRestClientProvider>(x => x.GetRequiredService<DiscordSocketClient>())
            .AddSingleton<InteractionService>()
            .AddSingleton<SimulatorRadio>()
            .AddSingleton<Statistics>()
            .AddHostedService<DiscordClientHost>()
            .AddHostedService<Status>()
            .AddHostedService<Monitor>()
            .AddLavalink()
            .AddInactivityTracking()
            .ConfigureLavalink(options =>
            {
                options.BaseAddress = new Uri($"http://{Environment.GetEnvironmentVariable("LAVALINK_HOST")}/");
                options.Passphrase = Environment.GetEnvironmentVariable("LAVALINK_AUTH")!;
            })
            .Configure<IdleInactivityTrackerOptions>(config => { config.Timeout = TimeSpan.FromHours(1); })
            .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }
}