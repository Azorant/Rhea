using Discord;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;

namespace Rhea.Bot.Models;

public sealed class CustomPlayer : VoteLavalinkPlayer
{
    public CustomPlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties)
        : base(properties)
    {
        TextChannel = properties.Options.Value.TextChannel;
    }
    public IMessageChannel TextChannel { get; set; }
 

    public static ValueTask<CustomPlayer> CreatePlayerAsync(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(properties);

        return ValueTask.FromResult(new CustomPlayer(properties));
    }
}