import { SlashCommandBuilder, CommandInteraction } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import { generateQueue } from '../images.js';
import type { Rhea } from '../index.js';
import { createPlayer } from '../utils.js';

export default class QueueCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('queue').setDescription("See what's up next").toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (!player.playing) return await interaction.reply("I'm not playing anything.");
    const file = await generateQueue(player.queue);

    return interaction.reply({
      files: [file],
    });
  }
}
