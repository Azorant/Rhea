namespace Rhea.Models;

public class TrackContext
{
    public TrackContext(string requester)
    {
        Requester = requester;
    }

    public string Requester { get; set; }
}