import { SlashCommandBuilder, CommandInteraction } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import type { Rhea } from '../index.js';
import { createPlayer, hasPermission } from '../utils.js';

export default class PauseCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('pause').setDescription('Pause player').toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (!player.playing) return await interaction.reply("I'm not playing anything.");

    const permission = await hasPermission(interaction, player);
    if (!permission) return await interaction.reply('You must either be alone in the channel, have a role named `DJ` or have the permission `Move Members` to pause the bot.');
    if (!player.paused) await player.pause();
    await interaction.reply('⏸️ **Paused**');
  }
}
