using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Hosting;
using Rhea.Models;

namespace Rhea.Services;

public class Positioning(IAudioService lavalink, RedisService redis) : IHostedService, IDisposable
{
    private Timer timer = null!;

    public void Dispose()
    {
        timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(SavePosition, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private async void SavePosition(object? state)
    {
        foreach (var lavalinkPlayer in lavalink.Players.Players)
        {
            var player = (IQueuedLavalinkPlayer)lavalinkPlayer;
            if (player.State is not PlayerState.Playing) continue;

            await redis.SetPlayingAsync(player.GuildId, new PlayingTrack((EnrichedTrack)player.CurrentItem!, player.VoiceChannelId, player.Position?.Position ?? TimeSpan.Zero));
        }
    }
}