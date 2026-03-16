import { SlashCommandBuilder, CommandInteraction } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import type { Rhea } from '../index.js';
import { createPlayer } from '../utils.js';

export default class ShuffleCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('shuffle').setDescription('Shuffle the queue').toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (!player.playing) return await interaction.reply("I'm not playing anything.");

    player.queue.shuffle();
    await interaction.reply('🔀 **Queue shuffled**');
  }
}
