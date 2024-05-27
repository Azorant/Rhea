using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest.Entities.Tracks;
using Rhea.Models;
using Rhea.Services;

namespace Rhea.Modules;

[CommandContextType(InteractionContextType.Guild)]
public class MediaModule(IAudioService lavalink, SimulatorRadio simulatorRadio, Statistics stats) : BaseModule(lavalink)
{
    [SlashCommand("play", "Play some music")]
    public async Task Play([Summary(description: "A search term or url")] string search)
    {
        var member = Context.Guild.GetUser(Context.User.Id);
        if (member.VoiceState == null)
        {
            await RespondAsync("You must be in a voice channel to run this command.", ephemeral: true);
            return;
        }

        var player = await GetPlayer();

        if (player == null || player.VoiceChannelId != member.VoiceChannel.Id)
        {
            await RespondAsync("You must be in the same voice channel as me to run this command.", ephemeral: true);
            return;
        }

        await DeferAsync();

        var searchResponse = await lavalink.Tracks.LoadTracksAsync(search, Uri.IsWellFormedUriString(search, UriKind.Absolute)
            ? TrackSearchMode.None
            : TrackSearchMode.YouTube);

        if (searchResponse.IsFailed || !searchResponse.HasMatches)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = $"Unable to find anything for `{Format.Sanitize(search)}`");
            return;
        }

        if (searchResponse.IsPlaylist)
        {
            stats.TracksLoaded?.Inc(searchResponse.Tracks.Length);
            await player.Queue.AddRangeAsync(searchResponse.Tracks.Select(lavalinkTrack => new EnrichedTrack(lavalinkTrack, DiscordClientHost.DisplayName(Context.User))).ToList());
            var embed = new EmbedBuilder()
                .WithTitle("Queued Playlist")
                .WithUrl(search)
                .WithImageUrl("attachment://playlist.png")
                .WithColor(Color.Blue)
                .WithFooter(DiscordClientHost.DisplayName(Context.User), Context.User.GetAvatarUrl()).Build();

            if (player.State is not PlayerState.Playing)
            {
                var nextTrack = await player.Queue.TryDequeueAsync();
                if (nextTrack != null) await player.PlayAsync(nextTrack, false);
            }

            var image = await ImageGenerator.GeneratePlaylist(searchResponse.Playlist, searchResponse.Tracks);

            await ModifyOriginalResponseAsync(m =>
            {
                m.Embed = embed;
                m.Attachments = new List<FileAttachment>() { image };
            });
            return;
        }

        var result = new EnrichedTrack(searchResponse.Tracks.First(), DiscordClientHost.DisplayName(Context.User));
        if (player.State is PlayerState.Playing or PlayerState.Paused)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Queued Track")
                .WithUrl(result.Track.Uri!.AbsoluteUri)
                .WithImageUrl("attachment://cover.png")
                .WithColor(Color.Blue)
                .WithFooter(DiscordClientHost.DisplayName(Context.User), Context.User.GetAvatarUrl());
            var image = await ImageGenerator.GenerateSingle(new TrackMetadata()
            {
                Artist = result.Track.Author,
                ArtworkUri = result.Track.ArtworkUri?.AbsoluteUri,
                Title = result.Track.Title,
                Duration = result.Track.Duration,
                Livestream = result.Track.IsLiveStream,
                QueuePosition = player.Queue.Count + 1,
                TimeToPlay = new TimeSpan(player.Queue.Sum(t => t.Track!.Duration.Ticks) + player.CurrentTrack!.Duration.Ticks - player.Position!.Value.Position.Ticks),
                Requester = DiscordClientHost.DisplayName(Context.User)
            });

            stats.TracksLoaded?.Inc();
            await player.Queue.AddAsync(result);

            await ModifyOriginalResponseAsync(m =>
            {
                m.Embed = embed.Build();
                m.Attachments = new List<FileAttachment>() { image };
            });
        }
        else
        {
            var embed = new EmbedBuilder()
                .WithTitle("Now Playing")
                .WithUrl(result.Track.Uri!.AbsoluteUri)
                .WithImageUrl("attachment://cover.png")
                .WithColor(Color.Green)
                .WithFooter(DiscordClientHost.DisplayName(Context.User), Context.User.GetAvatarUrl());
            var image = await ImageGenerator.GenerateSingle(new TrackMetadata()
            {
                Artist = result.Track.Author,
                ArtworkUri = result.Track.ArtworkUri?.AbsoluteUri,
                Title = result.Track.Title,
                Duration = result.Track.Duration,
                Livestream = result.Track.IsLiveStream,
                Requester = DiscordClientHost.DisplayName(Context.User)
            });

            stats.TracksLoaded?.Inc();
            await player.PlayAsync(result);
            await ModifyOriginalResponseAsync(m =>
            {
                m.Embed = embed.Build();
                m.Attachments = new List<FileAttachment>() { image };
            });
        }
    }

    [SlashCommand("simulator-radio", "Play music from Simulator Radio")]
    public async Task SimulatorRadio()
    {
        var member = Context.Guild.GetUser(Context.User.Id);
        if (member.VoiceState == null)
        {
            await RespondAsync("You must be in a voice channel to run this command.", ephemeral: true);
            return;
        }

        var player = await GetPlayer();

        if (player == null || player.VoiceChannelId != member.VoiceChannel.Id)
        {
            await RespondAsync("You must be in the same voice channel as me to run this command.", ephemeral: true);
            return;
        }

        await DeferAsync();

        var track = await lavalink.Tracks.LoadTrackAsync(simulatorRadio.url(), TrackSearchMode.None);
        if (track is null)
        {
            await ModifyOriginalResponseAsync(properties => properties.Content = "Unable to stream Simulator Radio");
            return;
        }

        var result = new EnrichedTrack(track, DiscordClientHost.DisplayName(Context.User));

        var embed = new EmbedBuilder()
            .WithAuthor("Now Playing")
            .WithTitle("Simulator Radio")
            .WithUrl("https://simulatorradio.com")
            .AddField("Song", simulatorRadio.song, true)
            .AddField("Artist", simulatorRadio.artist, true)
            .WithThumbnailUrl(simulatorRadio.artwork)
            .WithColor(Color.Green)
            .WithFooter(DiscordClientHost.DisplayName(Context.User), Context.User.GetAvatarUrl());

        await player.PlayAsync(result, false);

        await ModifyOriginalResponseAsync(properties => properties.Embed = embed.Build());
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

        var player = await GetPlayer();

        if (player == null || player.VoiceChannelId != member.VoiceChannel.Id)
        {
            await RespondAsync("You must be in the same voice channel as me to run this command.", ephemeral: true);
            return;
        }

        if (player.State is not PlayerState.Playing)
        {
            await RespondAsync("I'm not playing anything");
            return;
        }

        if (player.CurrentTrack?.Uri != null && player.CurrentTrack.Uri.AbsoluteUri.StartsWith("https://simulatorradio.stream/stream"))
        {
            var embed = new EmbedBuilder()
                .WithAuthor("Currently Playing")
                .WithTitle("Simulator Radio")
                .WithUrl("https://simulatorradio.com")
                .WithDescription(
                    $"{simulatorRadio.song} by {simulatorRadio.artist}")
                .WithThumbnailUrl(simulatorRadio.artwork)
                .WithColor(Color.Blue);

            await RespondAsync(embed: embed.Build());
        }
        else
        {
            var embed = new EmbedBuilder()
                .WithTitle("Currently Playing")
                .WithUrl(player.CurrentTrack!.Uri!.AbsoluteUri)
                .WithImageUrl("attachment://cover.png")
                .WithColor(Color.Blue)
                .WithFooter(DiscordClientHost.DisplayName(Context.User), Context.User.GetAvatarUrl());
            var image = await ImageGenerator.GenerateSingle(new TrackMetadata()
            {
                Artist = player.CurrentTrack.Author,
                ArtworkUri = player.CurrentTrack.ArtworkUri?.AbsoluteUri,
                Title = player.CurrentTrack.Title,
                Duration = player.CurrentTrack.Duration,
                Livestream = player.CurrentTrack.IsLiveStream,
                CurrentPosition = player.Position!.Value.Position,
                Requester = ((EnrichedTrack)player.CurrentItem!).Requester
            });
            await RespondWithFileAsync(attachment: image, embed: embed.Build());
        }
    }

    private Embed QueueEmbed(VoteLavalinkPlayer player, int page = 0)
    {
        string loop;
        switch (player.RepeatMode)
        {
            case TrackRepeatMode.Track:
                loop = "Looping Track";
                break;
            case TrackRepeatMode.Queue:
                loop = "Looping Queue";
                break;
            case TrackRepeatMode.None:
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
                    ? $"{player.CurrentTrack!.Title} | {FormatTime(player.Position!.Value.Position)}/{FormatTime(player.CurrentTrack.Duration)} | {((EnrichedTrack)player.CurrentItem!).Requester}"
                    : "Nothing playing")
            .AddField("Up Next", string.Join("\n", tracks.Select(track => $"{track.Track!.Title} | {FormatTime(track.Track!.Duration)} | {((EnrichedTrack)track).Requester}")))
            .WithFooter($"{player.Queue.Count:N0} Tracks | {loop}")
            .WithColor(Color.Blue)
            .Build();

        return embed;
    }

    [SlashCommand("queue", "Show what's in queue")]
    public async Task QueueCommand()
    {
        var player = await GetPlayer(PlayerChannelBehavior.None);
        if (player == null || player.Queue.IsEmpty)
        {
            await RespondAsync("Nothing in queue");
            return;
        }

        var embed = QueueEmbed(player);
        await RespondAsync(embed: embed);
    }
}