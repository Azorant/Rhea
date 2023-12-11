using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rhea.Services;
using Serilog;

namespace Rhea;

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
            .AddSingleton<InteractionService>()
            .AddSingleton<SimulatorRadio>()
            .AddSingleton<RedisService>()
            .AddHostedService<Positioning>()
            .AddHostedService<DiscordClientHost>()
            .AddHostedService<Status>()
            .AddLavalink()
            .AddInactivityTracking()
            .ConfigureLavalink(options =>
            {
                options.BaseAddress = new Uri($"http://{Environment.GetEnvironmentVariable("LAVALINK_HOST")}/");
                options.Passphrase = Environment.GetEnvironmentVariable("LAVALINK_AUTH")!;
            })
            .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }
}