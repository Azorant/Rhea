using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Rhea.Models;

namespace Rhea.Modules;

public class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAudioService lavalink;

    protected BaseModule(IAudioService lavalink)
    {
        this.lavalink = lavalink;
    }

    protected async ValueTask<VoteLavalinkPlayer?> GetPlayer(PlayerChannelBehavior joinBehavior = PlayerChannelBehavior.Join)
    {
        var member = Context.Guild.GetUser(Context.User.Id);
        var permissions = Context.Guild.CurrentUser.GetPermissions(member.VoiceChannel);
        if (!permissions.Connect)
        {
            throw new Exception($"Unable to connect to {member.VoiceChannel.Mention}");
        }
        
        var result = await lavalink.Players.RetrieveAsync(Context, playerFactory: PlayerFactory.Vote, new PlayerRetrieveOptions(joinBehavior));
        if (!result.IsSuccess && result.Status != PlayerRetrieveStatus.BotNotConnected) throw new Exception($"Unable to retrieve player: {result.Status}");
        return result.Player;
    }

    protected string FormatTime(TimeSpan time)
        => time.ToString(@"hh\:mm\:ss").TrimStart('0', ':');

    protected bool IsPrivileged(SocketGuildUser Member)
        => Member.GetPermissions(Member.VoiceChannel).MoveMembers || Member.Roles.FirstOrDefault(role => role.Name.ToLower() == "dj") != null ||
           !Member.VoiceChannel.ConnectedUsers.Any(user => !user.IsBot && user.Id != Member.Id);
}