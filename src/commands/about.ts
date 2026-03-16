import { ActionRowBuilder, ButtonBuilder, ButtonStyle, Colors, CommandInteraction, SlashCommandBuilder, version } from 'discord.js';
import type { Rhea } from '../index.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import { metadata } from 'rainlink';
import { generateInvites } from '../utils.js';

export default class AboutCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder().setName('about').setDescription('Info about the bot').toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: CommandInteraction) {
    const azorant = await context.client.users.fetch('160168328520794112');

    interaction.reply({
      embeds: [
        {
          description: 'Rhea is a no bullshit music bot.',
          color: Colors.DarkVividPink,
          fields: [
            {
              name: 'Guilds',
              value: context.client.guilds.cache.size.toString(),
              inline: true,
            },
            {
              name: 'Players',
              value: context.lavalink.players.size.toString(),
              inline: true,
            },
            {
              name: 'Library',
              value: `Discord.js ${version}\nRainlink ${typeof metadata.version === 'object' ? metadata.version.version : metadata.version}`,
              inline: true,
            },
          ],
          footer: {
            text: `Developed by ${azorant.tag}`,
            icon_url: azorant.avatarURL() ?? azorant.defaultAvatarURL,
          },
        },
      ],
      components: [
        new ActionRowBuilder()
          .addComponents(...generateInvites(), new ButtonBuilder().setURL('https://github.com/Azorant/Rhea').setStyle(ButtonStyle.Link).setLabel('GitHub'))
          .toJSON(),
      ],
    });
  }
}
