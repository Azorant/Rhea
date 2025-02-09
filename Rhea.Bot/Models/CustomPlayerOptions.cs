using Discord;
using Lavalink4NET.Players.Vote;

namespace Rhea.Bot.Models;

public sealed record class CustomPlayerOptions : VoteLavalinkPlayerOptions
{
    public IMessageChannel TextChannel { get; set; }
}