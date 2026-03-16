import { SlashCommandBuilder, CommandInteraction, type VoiceBasedChannel, ButtonInteraction, MessageFlags } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import type { Rhea } from '../index.js';
import { createPlayer, hasPermission } from '../utils.js';
import pluralize from 'pluralize';

export default class SkipCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('skip').setDescription('Skip the song').toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction, trackId?: string) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (trackId && player.queue.current?.identifier !== trackId)
      return await interaction.reply({
        flags: MessageFlags.Ephemeral,
        content: 'This song is no longer playing',
      });
    if (!player.playing) return await interaction.reply("I'm not playing anything.");

    const permission = await hasPermission(interaction, player);
    if (!permission) {
      const channel = interaction.guild?.channels.cache.get(player.voiceId!) as VoiceBasedChannel;
      const memberCount = channel.members.filter((m) => !m.user.bot).size;
      if (memberCount !== 1) {
        const votes = context.votes.get(channel.id) || [];
        const votesNeeded = Math.ceil(memberCount * 0.66);
        if (votes.length < votesNeeded) {
          if (!votes.includes(interaction.user.id)) {
            votes.push(interaction.user.id);
            context.votes.set(channel.id, votes);
          }
          return await interaction.reply(
            `${trackId ? `${interaction.user} voted to skip\n` : ''}Need **${votesNeeded - votes.length}** more ${pluralize('vote', votesNeeded - votes.length)} to skip.`,
          );
        }
      }
    }
    await player.skip();
    await interaction.reply(':fast_forward: **Skipped** :thumbsup:');
  }

  async executeComponent(context: Rhea, interaction: ButtonInteraction) {
    const id = interaction.customId.split(':').slice(1).join(':');
    await this.executeCommand(context, interaction as unknown as CommandInteraction, id);
  }
}
