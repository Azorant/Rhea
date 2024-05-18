using System.Reflection;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Rhea.Models;
using Rhea.Services;

namespace Rhea.Modules;

public class MiscModule(IAudioService lavalink) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("about", "Information about the bot")]
    public async Task AboutCommand()
    {
        var library = Assembly.GetAssembly(typeof(InteractionModuleBase))!.GetName();
        var korrdyn = Context.Client.GetUser(160168328520794112);
        var embed = new EmbedBuilder().WithAuthor(Context.Client.CurrentUser.Username, Context.Client.CurrentUser.GetAvatarUrl())
            .WithDescription("Rhea was created to be a free, easy to use, no ads music bot.")
            .AddField("Guilds", Context.Client.Guilds.Count.ToString("N0"), true)
            .AddField("Users", Context.Client.Guilds.Select(guild => guild.MemberCount).Sum().ToString("N0"), true)
            .AddField("Library", $"Discord.Net {library.Version!.ToString()}", true)
            .AddField("Players", lavalink.Players.Players.Count().ToString("N0"), true)
            .AddField("Developer", $"{DiscordClientHost.DisplayName(korrdyn)}", true)
            .AddField("Links",
                $"[GitHub](https://github.com/Korrdyn/Rhea)\n[Support](https://discord.gg/{Environment.GetEnvironmentVariable("DISCORD_INVITE")})\n[Patreon](https://patreon.com/Korrdyn)",
                true)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .WithFooter(DiscordClientHost.DisplayName(Context.User), Context.User.GetAvatarUrl())
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("invite", "Invite the bot")]
    public async Task InviteCommand()
        => await RespondAsync(
            $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot%20applications.commands");

    [SlashCommand("help", "Getting started")]
    public async Task HelpCommand()
    {
        var username = Context.Client.CurrentUser.Username;
        var commands = await (DiscordClientHost.IsDebug() ? Context.Guild.GetApplicationCommandsAsync() : Context.Client.GetGlobalApplicationCommandsAsync());
        var playCommand = commands.First(c => c.Name == "play");
        var queueCommand = commands.First(c => c.Name == "queue");
        var npCommand = commands.First(c => c.Name == "np");
        var shuffleCommand = commands.First(c => c.Name == "shuffle");
        var smCommand = commands.First(c => c.Name == "simulator-radio");

        var embed = new EmbedBuilder()
            .WithTitle("Getting Started")
            .WithColor(Color.Blue)
            .WithDescription(
                $"{username} supports media from YouTube, Spotify, SoundCloud, Apple Music, and Twitch.\n\nGetting started it pretty easy! Just join a voice channel that {username} has access to and run the </play:{playCommand.Id}> command to queue a song or playlist.\n\nCheckout what's next in queue with the </queue:{queueCommand.Id}> command. If you're not feeling the next few songs or want to change things up, run the </shuffle:{shuffleCommand.Id}> command to shuffle the queue.\n\nIf you're curious what's playing or how much time is left in the song, you can use the </np:{npCommand.Id}> command to get info about what is currently playing.\n\nIf you don't really know what to queue up, run the </simulator-radio:{smCommand.Id}> command to start listening to [Simulator Radio](https://simulatorradio.com), an internet radio station. If you're wondering or didn't catch the song name from the DJ, run </np:{npCommand.Id}> to get the currently playing song on SM.\n\n If you ever need help with {username} or have feedback/an idea, join the [support server](https://discord.gg/{Environment.GetEnvironmentVariable("DISCORD_INVITE")}).")
            .WithFooter(DiscordClientHost.DisplayName(Context.User), Context.User.GetAvatarUrl())
            .Build();

        await RespondAsync(embed: embed);
    }
}