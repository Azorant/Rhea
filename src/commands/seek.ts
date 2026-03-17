import { SlashCommandBuilder, ChatInputCommandInteraction } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import type { Rhea } from '../index.js';
import { createPlayer } from '../utils.js';
import { fromTimestamp } from '@azorant/time';

export default class SeekCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder()
    .setName('seek')
    .setDescription('Seek timestamp in song')
    .addStringOption((option) => option.setName('timestamp').setDescription('Timestamp to seek to').setRequired(true))
    .toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: ChatInputCommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (!player.playing) return await interaction.reply("I'm not playing anything.");
    if (!player.queue.current?.isSeekable) return await interaction.reply("You can't seek on the current song.");
    const timestamp = interaction.options.get('timestamp', true).value as string;
    await player.seek(fromTimestamp(timestamp));
    await interaction.reply(`**Seeked to** \`${timestamp}\``);
  }
}
