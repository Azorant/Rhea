namespace Rhea.Bot.Models;

public class TrackMetadata
{
    public string? ArtworkUri { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required TimeSpan Duration { get; set; }
    public required bool Livestream { get; set; }
    public TimeSpan? CurrentPosition { get; set; }
    public int? QueuePosition { get; set; }
    public TimeSpan? TimeToPlay { get; set; }
    public string? Requester { get; set; }
}