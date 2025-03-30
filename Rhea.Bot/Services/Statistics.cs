using Prometheus;
using Serilog;

namespace Rhea.Bot.Services;

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
        Log.Information("Prometheus metrics started");
        Guilds = Metrics.CreateGauge("rhea_guilds", "Guilds bot is in");
        Commands = Metrics.CreateCounter("rhea_commands_total", "Commands ran", labelNames: ["command"]);
        Latency = Metrics.CreateGauge("rhea_latency", "Websocket latency");
        TracksPlayed = Metrics.CreateCounter("rhea_tracks_played_total", "Total number of tracks played");
        TracksLoaded = Metrics.CreateCounter("rhea_tracks_loaded_total", "Total number of tracks loaded");
        TracksQueued = Metrics.CreateGauge("rhea_tracks_queued", "Current number of tracks queued");
        Players = Metrics.CreateGauge("rhea_players", "Number of players");
        Server = new MetricServer(3400);
        Server.Start();
    }
}