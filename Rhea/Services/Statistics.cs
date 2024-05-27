using Prometheus;
using Serilog;

namespace Rhea.Services;

public class Statistics
{
    private MetricServer? Server { get; set; }
    public Gauge? Guilds { get; set; }
    public Counter? Commands { get; set; }
    public Gauge? Latency { get; set; }
    public Counter? TracksPlayed { get; set; }
    public Counter? TracksLoaded { get; set; }
    public Gauge? TracksQueued { get; set; }
    public Gauge? Players { get; set; }

    public Statistics()
    {
        if (Environment.GetEnvironmentVariable("PROMETHEUS_ENABLED") != "true")
        {
            Log.Information("Prometheus metrics disabled");
            return;
        }

        Log.Information("Prometheus metrics started");
        var prefix = Environment.GetEnvironmentVariable("PROMETHEUS_PREFIX") ?? "rhea";
        Guilds = Metrics.CreateGauge($"{prefix}_guilds", "Guilds bot is in");
        Commands = Metrics.CreateCounter($"{prefix}_commands_total", "Commands ran", labelNames: ["command"]);
        Latency = Metrics.CreateGauge($"{prefix}_latency", "Websocket latency");
        TracksPlayed = Metrics.CreateCounter($"{prefix}_tracks_played_total", "Total number of tracks played");
        TracksLoaded = Metrics.CreateCounter($"{prefix}_tracks_loaded_total", "Total number of tracks loaded");
        TracksQueued = Metrics.CreateGauge($"{prefix}_tracks_queued", "Current number of tracks queued");
        Players = Metrics.CreateGauge($"{prefix}_players", "Number of players");
        Server = new MetricServer(3400);
        Server.Start();
    }
}