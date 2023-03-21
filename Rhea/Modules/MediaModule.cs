using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Artwork;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Rhea.Models;

namespace Rhea.Modules;

public class MediaModule : BaseModule
{
    private readonly IAudioService lavalink;
    private readonly IArtworkService artwork;

    public MediaModule(IAudioService lavalink, IArtworkService artwork) : base(lavalink)
    {
        this.lavalink = lavalink;
        this.artwork = artwork;
    }

    [SlashCommand("play", "Play some music")]
    public async Task Play([Summary(description: "A search term or url")] string search)
    {
        var member = Context.Guild.GetUser(Context.User.Id);
        if (member.VoiceState == null)
        {
            await RespondAsync("You must be in a voice channel to run this command.", ephemeral: true);
            return;
        }

        var player = await GetPlayer(Context.Guild.Id, member.VoiceChannel.Id);

        if (player.VoiceChannelId != member.VoiceChannel.Id)
        {
            await RespondAsync("You must be in the same voice channel as me to run this command.", ephemeral: true);
            return;
        }

        await DeferAsync();

        var searchResponse = await lavalink.LoadTracksAsync(search, Uri.IsWellFormedUriString(search, UriKind.Absolute)
            ? SearchMode.None
            : SearchMode.YouTube);

        if (searchResponse.LoadType is TrackLoadType.LoadFailed or TrackLoadType.NoMatches)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = $"Unable to find anything for `{Format.Sanitize(search)}`");
            return;
        }

        if (!string.IsNullOrWhiteSpace(searchResponse.PlaylistInfo?.Name))
        {
            player.Queue.AddRange(searchResponse.Tracks!.Select(lavalinkTrack =>
            {
                lavalinkTrack.Context = new TrackContext($"{Context.User.Username}#{Context.User.Discriminator}");
                return lavalinkTrack;
            }));
            var embed = new EmbedBuilder()
                .WithAuthor("Queued Playlist")
                .WithTitle(searchResponse.PlaylistInfo.Name)
                .WithUrl(search)
                .AddField("Tracks", searchResponse.Tracks!.Count(), true)
                .AddField("Playlist length", FormatTime(new TimeSpan(searchResponse.Tracks!.Sum(t => t.Duration.Ticks))), true)
                .WithColor(Color.Blue)
                .WithFooter($"{Context.User.Username}#{Context.User.Discriminator}", Context.User.GetAvatarUrl()).Build();

            if (player.State is not PlayerState.Playing or PlayerState.Paused && player.Queue.TryDequeue(out var track)) await player.PlayAsync(track!, enqueue: false);

            await ModifyOriginalResponseAsync(properties => properties.Embed = embed);
        }
        else
        {
            var track = searchResponse.Tracks!.First();
            track.Context = new TrackContext($"{Context.User.Username}#{Context.User.Discriminator}");

            var art = await artwork.ResolveAsync(track);

            if (player.State is PlayerState.Playing or PlayerState.Paused)
            {
                var embed = new EmbedBuilder()
                    .WithAuthor("Queued Track")
                    .WithTitle(track.Title)
                    .WithUrl(track.Uri!.AbsoluteUri)
                    .AddField("Channel", track.Author, true)
                    .AddField("Duration", track.IsLiveStream
                        ? "Live stream"
                        : FormatTime(track.Duration), true)
                    .AddField("Time until playing",
                        FormatTime(new TimeSpan(player.Queue.Sum(t => t.Duration.Ticks) + player.CurrentTrack!.Duration.Ticks - player.Position.Position.Ticks)),
                        true)
                    .AddField("Queue position", player.Queue.Count + 1)
                    .WithColor(Color.Blue)
                    .WithFooter($"{Context.User.Username}#{Context.User.Discriminator}", Context.User.GetAvatarUrl());

                if (art != null) embed.WithThumbnailUrl(art.AbsoluteUri);

                player.Queue.Add(track);

                await ModifyOriginalResponseAsync(m => m.Embed = embed.Build());
            }
            else
            {
                var embed = new EmbedBuilder()
                    .WithAuthor("Now Playing")
                    .WithTitle(track.Title)
                    .WithUrl(track.Uri!.AbsoluteUri)
                    .AddField("Channel", track.Author, true)
                    .AddField("Duration", track.IsLiveStream
                        ? "Live stream"
                        : FormatTime(track.Duration), true)
                    .WithColor(Color.Green)
                    .WithFooter($"{Context.User.Username}#{Context.User.Discriminator}", Context.User.GetAvatarUrl());

                if (art != null) embed.WithThumbnailUrl(art.AbsoluteUri);

                await player.PlayAsync(track);

                await ModifyOriginalResponseAsync(properties => properties.Embed = embed.Build());
            }
        }
    }


    [SlashCommand("np", "Show what is currently playing")]
    public async Task NowPlayingCommand()
    {
        var member = Context.Guild.GetUser(Context.User.Id);
        if (member.VoiceState == null)
        {
            await RespondAsync("You must be in a voice channel to run this command.", ephemeral: true);
            return;
        }

        var player = await GetPlayer(Context.Guild.Id, member.VoiceChannel.Id);

        if (player.VoiceChannelId != member.VoiceChannel.Id)
        {
            await RespondAsync("You must be in the same voice channel as me to run this command.", ephemeral: true);
            return;
        }

        if (player.State is not PlayerState.Playing or PlayerState.Paused)
        {
            await RespondAsync("I'm not playing anything");
            return;
        }

        var bar = "";

        if (player.CurrentTrack!.IsLiveStream)
        {
            bar = "Live stream";
        }
        else
        {
            var progress = (int)Math.Floor((decimal)player.Position.Position.Ticks / player.CurrentTrack!.Duration.Ticks * 100 / 4);
            if (progress - 1 > 0) bar += new string('▬', progress - 1);
            bar += "🔘";
            bar += new string('▬', 25 - progress);
            bar += $"\n\n{FormatTime(player.Position.Position)} / {FormatTime(player.CurrentTrack.Duration)}";
        }

        var art = await artwork.ResolveAsync(player.CurrentTrack);

        var embed = new EmbedBuilder()
            .WithTitle("Currently playing")
            .WithDescription(
                $"[{player.CurrentTrack.Title}]({player.CurrentTrack.Uri})\n\n{bar}")
            .WithColor(Color.Blue);

        if (art != null) embed.WithThumbnailUrl(art.AbsoluteUri);

        await RespondAsync(embed: embed.Build());
    }

    private Embed QueueEmbed(VoteLavalinkPlayer player, int page = 0)
    {
        string loop;
        switch (player.LoopMode)
        {
            case PlayerLoopMode.Track:
                loop = "Looping Track";
                break;
            case PlayerLoopMode.Queue:
                loop = "Looping Queue";
                break;
            case PlayerLoopMode.None:
            default:
                loop = "Not Looping";
                break;
        }

        var pages = Math.Ceiling((decimal)player.Queue.Count / 10);
        if (page * 10 > player.Queue.Count) page = (int)pages - 1;

        var tracks = player.Queue.Skip(page * 10).Take(10).ToList();
        var embed = new EmbedBuilder()
            .AddField("Currently Playing",
                player.CurrentTrack != null
                    ? $"{player.CurrentTrack!.Title} | {FormatTime(player.Position.Position)}/{FormatTime(player.CurrentTrack.Duration)} | {((TrackContext)player.CurrentTrack.Context!).Requester}"
                    : "Nothing playing")
            .AddField("Up Next", string.Join("\n", tracks.Select(track => $"{track.Title} | {FormatTime(track.Duration)} | {((TrackContext)track.Context!).Requester}")))
            .WithFooter($"{player.Queue.Count:N0} Tracks | {loop}")
            .WithColor(Color.Blue)
            .Build();

        return embed;
    }

    [SlashCommand("queue", "Show what's in queue")]
    public async Task QueueCommand()
    {
        var player = lavalink.GetPlayer<VoteLavalinkPlayer>(Context.Guild.Id);
        if (player == null || player.Queue.IsEmpty)
        {
            await RespondAsync("Nothing in queue");
            return;
        }

        var embed = QueueEmbed(player);
        await RespondAsync(embed: embed);
    }
}