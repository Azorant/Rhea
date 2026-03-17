import { SlashCommandBuilder, ChatInputCommandInteraction } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import type { Rhea } from '../index.js';
import { createPlayer } from '../utils.js';

export default class VolumeCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder()
    .setName('volume')
    .setDescription('Set the players volume')
    .addIntegerOption((option) => option.setName('volume').setDescription('Volume level').setMinValue(1).setMaxValue(100).setRequired(true))
    .toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: ChatInputCommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (!player.playing) return await interaction.reply("I'm not playing anything.");
    if (!player.queue.current?.isSeekable) return await interaction.reply("You can't seek on the current song.");
    const level = interaction.options.get('volume', true).value as number;
    player.setVolume(level);
    await interaction.reply(`\ud83d\udd0a Volume set to ${level}%`);
  }
}
