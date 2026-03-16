import { SlashCommandBuilder, CommandInteraction } from 'discord.js';
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
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (!player.playing) return await interaction.reply("I'm not playing anything.");
    const mode = interaction.options.get('mode', true).value as RainlinkLoopMode;
    player.setLoop(mode);
    switch (mode) {
      case RainlinkLoopMode.NONE:
        await interaction.reply('🔂 **Loop disabled**');
        break;
      case RainlinkLoopMode.SONG:
        await interaction.reply('🔂 **Loop song**');
        break;
      case RainlinkLoopMode.QUEUE:
        await interaction.reply('🔂 **Loop queue**');
        break;
    }
  }
}
