using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Player;

namespace Rhea.Modules;

public class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAudioService lavalink;

    protected BaseModule(IAudioService lavalink)
    {
        this.lavalink = lavalink;
    }

    protected async Task<VoteLavalinkPlayer> GetPlayer(ulong GuildID, ulong ChannelID)
        => lavalink.GetPlayer<VoteLavalinkPlayer>(GuildID) ?? await lavalink.JoinAsync<VoteLavalinkPlayer>(GuildID, ChannelID);

    protected string FormatTime(TimeSpan time)
        => time.ToString(@"hh\:mm\:ss").TrimStart('0', ':');

    protected bool IsPrivileged(SocketGuildUser Member)
        => Member.GetPermissions(Member.VoiceChannel).MoveMembers || Member.Roles.FirstOrDefault(role => role.Name.ToLower() == "dj") != null ||
           !Member.VoiceChannel.ConnectedUsers.Any(user => !user.IsBot && user.Id != Member.Id);
}