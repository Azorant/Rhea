using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Rhea.Bot.Models;
using Rhea.Bot.Services;

namespace Rhea.Bot.Modules;

public class BaseModule(IAudioService lavalink) : InteractionModuleBase<SocketInteractionContext>
{
    protected async ValueTask<VoteLavalinkPlayer?> GetPlayer(
        PlayerChannelBehavior joinBehavior = PlayerChannelBehavior.Join)
    {
        var member = Context.Guild.GetUser(Context.User.Id);
        var permissions = Context.Guild.CurrentUser.GetPermissions(member.VoiceChannel);
        if (!permissions.Connect)
        {
            throw new Exception($"Unable to connect to {member.VoiceChannel.Mention}");
        }
        
        var result = await lavalink.Players.RetrieveAsync(Context, PlayerFactory.Vote, new PlayerRetrieveOptions(joinBehavior));
        if (!result.IsSuccess && result.Status != PlayerRetrieveStatus.BotNotConnected)
            throw new Exception($"Unable to retrieve player: {result.Status}");
        return result.Player;
    }

    public static string FormatTime(TimeSpan time)
        => time.ToString(@"dd\:hh\:mm\:ss").TrimStart('0', ':');

    protected static bool IsPrivileged(SocketGuildUser member)
        => member.GetPermissions(member.VoiceChannel).MoveMembers ||
           member.Roles.FirstOrDefault(role => role.Name.ToLower() == "dj") != null ||
           !member.VoiceChannel.ConnectedUsers.Any(user => !user.IsBot && user.Id != member.Id);
}