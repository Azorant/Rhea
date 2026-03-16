import { ButtonBuilder, ButtonStyle, type CommandInteraction } from 'discord.js';
import type { Rhea } from './index.js';
import { type RainlinkPlayer } from 'rainlink';

export async function createPlayer(context: Rhea, interaction: CommandInteraction) {
  if (!interaction.inGuild()) return;
  const voiceChannelId = (await interaction.guild?.voiceStates.cache.get(interaction.user.id))?.channelId;
  if (!voiceChannelId) {
    await interaction.reply('You must be in a voice channel to run this command.');
    return;
  }
  const guildId = interaction.guild!.id;
  const existingPlayer = context.lavalink.players.get(guildId);
  if (existingPlayer?.voiceId) {
    if (existingPlayer.voiceId === voiceChannelId) return existingPlayer;
    await interaction.reply(`You must be in the same voice channel as me to run this command.`);
  }

  return context.lavalink.create({
    guildId,
    textId: interaction.channel!.id,
    voiceId: voiceChannelId,
    shardId: 0,
    volume: 50,
  });
}

export async function hasPermission(interaction: CommandInteraction, player: RainlinkPlayer) {
  const member = await interaction.guild?.members.fetch(interaction.user.id);
  if (!member) return false;

  if (member.roles.cache.find((r) => r.name.toLowerCase() === 'dj')) return true;

  const channel = await interaction.guild!.channels.fetch(player.voiceId!);
  const permissions = channel?.permissionsFor(member.user.id);
  if (permissions?.has('MoveMembers')) return true;

  return false;
}

export function generateInvites() {
  return process.env.BOTS!.split(',').map((bot) =>
    new ButtonBuilder()
      .setURL(`https://discord.com/api/oauth2/authorize?client_id=${bot.split('=')[1]}&scope=bot%20applications.commands`)
      .setStyle(ButtonStyle.Link)
      .setLabel(`Invite ${bot.split('=')[0]}`),
  );
}
