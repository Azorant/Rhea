import { SlashCommandBuilder, ChatInputCommandInteraction } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import type { Rhea } from '../index.js';
import { createPlayer } from '../utils.js';
import { RainlinkLoopMode } from 'rainlink';

export default class LoopCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder()
    .setName('loop')
    .setDescription('Loop the queue')
    .addStringOption((option) =>
      option
        .setName('mode')
        .setDescription('Loop mode')
        .addChoices([
          {
            name: 'None',
            value: RainlinkLoopMode.NONE,
          },
          {
            name: 'Song',
            value: RainlinkLoopMode.SONG,
          },
          {
            name: 'Queue',
            value: RainlinkLoopMode.QUEUE,
          },
        ])
        .setRequired(true),
    )
    .toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: ChatInputCommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;
    const mode = interaction.options.get('mode', true).value as RainlinkLoopMode;
    player.setLoop(mode);
    switch (mode) {
      case RainlinkLoopMode.NONE:
        await interaction.followUp('🔂 **Loop disabled**');
        break;
      case RainlinkLoopMode.SONG:
        await interaction.followUp('🔂 **Loop song**');
        break;
      case RainlinkLoopMode.QUEUE:
        await interaction.reply('🔂 **Loop queue**');
        break;
    }
  }
}
