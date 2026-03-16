import { ActionRowBuilder, CommandInteraction, SlashCommandBuilder } from 'discord.js';
import type { Rhea } from '../index.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import { generateInvites } from '../utils.js';
import pluralize from 'pluralize';

export default class InviteCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('invite').setDescription('Invite the bot').toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const buttons = generateInvites();
    interaction.reply({
      content: `There ${pluralize('are', buttons.length)} ${buttons.length} ${pluralize('instance', buttons.length)} of Rhea you can invite`,
      components: [new ActionRowBuilder().addComponents(...buttons).toJSON()],
    });
  }
}
