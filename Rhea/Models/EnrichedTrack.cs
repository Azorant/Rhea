using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;

namespace Rhea.Models;

public class EnrichedTrack(LavalinkTrack track, string requester) : ITrackQueueItem
{
    public string Requester { get; } = requester;
    public TrackReference Reference { get; } = new TrackReference(track);
    public LavalinkTrack Track => Reference.Track!;
}