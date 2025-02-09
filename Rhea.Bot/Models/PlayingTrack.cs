namespace Rhea.Bot.Models;

public class PlayingTrack(EnrichedTrack track, ulong channelID, TimeSpan position = default)
{
    public EnrichedTrack track { get; set; } = track;
    public TimeSpan position { get; set; } = position;
    public ulong channelID { get; set; } = channelID;
}