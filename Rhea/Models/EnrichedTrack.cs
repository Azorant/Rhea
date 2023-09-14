using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;

namespace Rhea.Models;

public class EnrichedTrack : ITrackQueueItem 
{
    public TrackReference Reference { get; }
    public LavalinkTrack Track => Reference.Track!;
    public string Requester { get; }

    public EnrichedTrack(LavalinkTrack track, string requester)
    {
        Reference = new TrackReference(track);
        Requester = requester;
    }
}