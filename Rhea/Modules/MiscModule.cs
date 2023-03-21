using System.Reflection;
using Discord;
using Discord.Interactions;

namespace Rhea.Modules;

public class MiscModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("about", "Information about the bot")]
    public async Task AboutCommand()
    {
        var library = Assembly.GetAssembly(typeof(InteractionModuleBase))!.GetName();
        var korrdyn = Context.Client.GetUser(160168328520794112);
        var embed = new EmbedBuilder().WithAuthor(Context.Client.CurrentUser.Username, Context.Client.CurrentUser.GetAvatarUrl())
            .AddField("Guilds", Context.Client.Guilds.Count.ToString("N0"), true)
            .AddField("Users", Context.Client.Guilds.Select(guild => guild.MemberCount).Sum().ToString("N0"), true)
            .AddField("Library", $"Discord.Net {library.Version!.ToString()}", true)
            .AddField("Developer", $"{korrdyn.Username}#{korrdyn.Discriminator}", true)
            .AddField("Links", $"[GitHub](https://github.com/Korrdyn/Rhea)\n[Support](https://discord.gg/{Environment.GetEnvironmentVariable("DISCORD_INVITE")})\n[Patreon](https://patreon.com/Korrdyn)", true)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .Build();

        await RespondAsync(embed: embed);
    }
    
    [SlashCommand("invite", "Invite the bot")]
    public async Task InviteCommand()
        => await RespondAsync(
            $"https://discord.com/api/oauth2/authorize?client_id={Context.Client.CurrentUser.Id}&scope=bot%20applications.commands");
}