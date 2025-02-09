using System.Collections;
using System.Collections.Immutable;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Lavalink4NET.Cluster.Extensions;
using Lavalink4NET.Cluster.Nodes;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rhea.Bot.Services;
using Serilog;

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
            .AddLavalinkCluster<DiscordClientWrapper>()
            .AddInactivityTracking()
            .ConfigureLavalinkCluster(options =>
            {
                var nodes = new List<LavalinkClusterNodeOptions>();
                foreach (DictionaryEntry v in Environment.GetEnvironmentVariables())
                {
                    if (((string)v.Key).StartsWith("LAVALINK_NODE"))
                    {
                        var parts = ((string)v.Value!).Split(',');
                        nodes.Add(new LavalinkClusterNodeOptions
                        {
                            Label = parts[0],
                            BaseAddress = new Uri(parts[1]),
                            Passphrase = parts[2]
                        });
                    }
                }

                options.Nodes = nodes.ToImmutableArray();
            })
            .Configure<IdleInactivityTrackerOptions>(config => { config.Timeout = TimeSpan.FromHours(1); })
            .AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }
}