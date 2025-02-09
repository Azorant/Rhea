using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Hosting;

namespace Rhea.Bot.Services;

public class Monitor(DiscordSocketClient client, IAudioService lavalink, Statistics stats) : IHostedService, IDisposable
{
    private Timer timer = null!;

    public void Dispose()
    {
        timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(Execute, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(10));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private void Execute(object? state)
    {
        stats.Latency?.Set(client.Latency);
        stats.Players?.Set(lavalink.Players.Players.Count());
        stats.TracksQueued?.Set(lavalink.Players.Players.Sum(player => ((QueuedLavalinkPlayer)player).Queue.Count));
    }
}