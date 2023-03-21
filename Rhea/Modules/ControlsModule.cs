using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Player;

namespace Rhea.Modules;

public class ControlsModule : BaseModule
{
    public ControlsModule(IAudioService lavalink) : base(lavalink) { }

    [SlashCommand("resume", "Resume playing")]
    public async Task ResumeCommand()
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

        if (player.State is not PlayerState.Paused)
        {
            await RespondAsync("I'm not paused");
            return;
        }

        if (!IsPrivileged(member))
        {
            await RespondAsync("You must either be alone in the channel, have a role named `DJ` or have the permission `Move Members` to resume the bot.");
            return;
        }

        await player.ResumeAsync();
        await RespondAsync("▶ **Resumed**");
    }

    [SlashCommand("pause", "Pause playing")]
    public async Task PauseCommand()
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

        if (player.State is not PlayerState.Playing)
        {
            await RespondAsync("I'm not playing anything");
            return;
        }

        if (!IsPrivileged(member))
        {
            await RespondAsync("You must either be alone in the channel, have a role named `DJ` or have the permission `Move Members` to pause the bot.");
            return;
        }

        await player.PauseAsync();
        await RespondAsync("⏸ **Paused**");
    }

    [SlashCommand("stop", "Stop playing")]
    public async Task StopCommand()
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

        if (!IsPrivileged(member))
        {
            await RespondAsync("You must either be alone in the channel, have a role named `DJ` or have the permission `Move Members` to stop the bot.");
            return;
        }

        await player.StopAsync(true);
        await RespondAsync("🛑 **Stopped and cleared queue**");
    }

    [SlashCommand("skip", "Skip to the next song")]
    public async Task SkipCommand()
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

        if (!IsPrivileged(member))
        {
            var result = await player.VoteAsync(member.Id, 0.66f);
            if (result.WasSkipped)
            {
                await RespondAsync(":fast_forward: **Skipped** :thumbsup:");
            }
            else
            {
                var threshold = result.TotalUsers * result.Percentage;
                await RespondAsync($"**Need {Math.Ceiling(threshold)} more votes to skip**");
            }
        }
        else
        {
            await player.SkipAsync();
            await RespondAsync(":fast_forward: **Skipped** :thumbsup:");
        }
    }

    [SlashCommand("shuffle", "Shuffle the queue")]
    public async Task ShuffleCommand()
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

        player.Queue.Shuffle();
        await RespondAsync("🔀 **Queue shuffled**");
    }

    [SlashCommand("seek", "Seek to a timestamp in the current track")]
    public async Task SeekCommand([Summary(description: "The timestamp to seek to")] string timestamp)
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

        if (player.State is not PlayerState.Playing)
        {
            await RespondAsync("I'm not playing anything.");
            return;
        }

        if (!player.CurrentTrack!.IsSeekable)
        {
            await RespondAsync("You can't seek on the current track.");
            return;
        }

        var parts = timestamp.Split(':');
        var parsedTimestamp = parts.Select(part => part.Length == 1
                ? $"0{part}"
                : part)
            .ToList();

        for (var i = parsedTimestamp.Count; i < 3; i++)
        {
            parsedTimestamp.Insert(0, "00");
        }

        var ts = TimeSpan.Parse(string.Join(':', parsedTimestamp));

        await player.SeekPositionAsync(ts);
        await RespondAsync($"**Seeked to** `{FormatTime(ts)}`");
    }

    [SlashCommand("loop", "Set whether or not the queue should loop")]
    public async Task LoopCommand(
        [Summary(description: "Loop mode"), Choice("Disable", (int)PlayerLoopMode.None), Choice("Track", (int)PlayerLoopMode.Track),
         Choice("Queue", (int)PlayerLoopMode.Queue)]
        int mode)
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

        player.LoopMode = (PlayerLoopMode)mode;
        switch ((PlayerLoopMode)mode)
        {
            case PlayerLoopMode.None:
                await RespondAsync("🔂 **Loop disabled**");
                break;
            case PlayerLoopMode.Track:
                await RespondAsync("🔂 **Loop track**");
                break;
            case PlayerLoopMode.Queue:
                await RespondAsync("🔂 **Loop queue**");
                break;
        }
    }
}