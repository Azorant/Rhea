import { SlashCommandBuilder, CommandInteraction } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import type { Rhea } from '../index.js';
import { createPlayer, hasPermission } from '../utils.js';

export default class StopCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('stop').setDescription('Stop player').toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    const permission = await hasPermission(interaction, player);
    if (!permission) return await interaction.reply('You must either be alone in the channel, have a role named `DJ` or have the permission `Move Members` to stop the bot.');
    await player.stop(true);
    await interaction.reply('🛑 **Stopped and cleared queue**');
  }
}
