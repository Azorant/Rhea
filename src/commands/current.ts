import { SlashCommandBuilder, CommandInteraction, ActionRowBuilder, ButtonBuilder, ButtonStyle } from 'discord.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import { generateSingle } from '../images.js';
import type { Rhea } from '../index.js';
import { createPlayer } from '../utils.js';

export default class CurrentCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('np').setDescription("See what's playing").toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;

    if (!player.playing) return await interaction.reply("I'm not playing anything.");
    const track = player.queue.current!;
    track.position = player.position; // TODO: When library is updated this will no longer be needed
    const file = await generateSingle(track);
    return interaction.followUp({
      components: [
        new ActionRowBuilder()
          .addComponents(
            new ButtonBuilder().setStyle(ButtonStyle.Link).setURL(track.uri!).setLabel('Song'),
            new ButtonBuilder().setCustomId(`skip:${track.identifier}`).setLabel('Skip').setStyle(ButtonStyle.Danger),
          )
          .toJSON(),
      ],
      files: [file],
    });
  }
}
