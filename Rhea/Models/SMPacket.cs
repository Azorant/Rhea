namespace Rhea.Models;

public class SMPacket
{
    public string type { get; set; } = null!;
    public NowPlaying now_playing { get; set; }
}

public class NowPlaying
{
    public string responder { get; set; }
    public string title { get; set; }
    public string artists { get; set; }
    public string art { get; set; }
    public int timestamp { get; set; }
}