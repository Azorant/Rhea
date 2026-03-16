import { BaseEvent } from 'displosion';
import type { Rhea } from '../index.js';
import { Colors, EmbedBuilder, time, TimestampStyles, type ClientEvents, type Guild } from 'discord.js';

export default class GuildDeleteEvent extends BaseEvent<Rhea> {
  event: keyof ClientEvents = 'guildDelete';
  once = false;
  async execute(context: Rhea, guild: Guild) {
    context.logger.info(`Left Guild: ${guild.name}`);
    if (!process.env.GUILD_CHANNEL) return;
    const channel = await context.client.channels.fetch(process.env.GUILD_CHANNEL);
    if (!channel?.isSendable()) return;
    const owner = await context.client.users.fetch(guild.ownerId);
    await channel.send({
      embeds: [
        new EmbedBuilder()
          .setTitle('Left Guild')
          .setDescription(
            `**Name:** ${guild.name}\n**ID:** ${guild.id}\n**Owner:** ${owner.tag}\n**Owner ID:** ${guild.ownerId}\n**Members:** ${guild.memberCount}\n**Created:** ${time(guild.createdAt, TimestampStyles.ShortDateTime)}`,
          )
          .setTimestamp(Date.now())
          .setColor(Colors.Red)
          .setThumbnail(guild.iconURL()),
      ],
    });
  }
}
