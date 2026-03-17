import { ActionRowBuilder, ButtonBuilder, ButtonStyle, ChatInputCommandInteraction, MediaGalleryBuilder, MessageFlags, SlashCommandBuilder, TextDisplayBuilder } from 'discord.js';
import type { Rhea } from '../index.js';
import { BaseCommand, type CommandPermissions } from 'displosion';
import { createPlayer } from '../utils.js';
import { RainlinkSearchResultType } from 'rainlink';
import { generatePlaylist, generateSingle } from '../images.js';

export default class PlayCommand extends BaseCommand<Rhea> {
  command = new SlashCommandBuilder()
    .setName('play')
    .setDescription('Play some music')
    .addStringOption((option) => option.setName('search').setDescription('Search query or url').setRequired(true))
    .toJSON();
  commandPermissions: CommandPermissions = [];
  ownerOnly = false;
  async executeCommand(context: Rhea, interaction: ChatInputCommandInteraction) {
    const player = await createPlayer(context, interaction);
    if (!player) return;
    await interaction.deferReply();

    const query = interaction.options.get('search', true).value!.toString();
    const searchResult = await context.lavalink.search(query, {
      requester: interaction.user.tag,
    });
    if (!searchResult.tracks.length) return await interaction.followUp('Unable to find anything');
    const isPlaying = player.playing;

    if (searchResult.type !== RainlinkSearchResultType.PLAYLIST) {
      const track = searchResult.tracks[0]!;
      const file = await generateSingle(
        track,
        isPlaying
          ? {
              position: player.queue.totalSize,
              duration: player.queue.duration + (player.queue.current ? player.queue.current.duration - player.queue.current.position : 0),
            }
          : undefined,
      );

      player.queue.add(track);
      if (!player.playing) await player.play();

      await interaction.followUp({
        components: [
          new TextDisplayBuilder().setContent(`**${isPlaying ? 'Song Queued' : 'Now Playing'}**`),
          new MediaGalleryBuilder().addItems((i) => i.setURL('attachment://image.png')),
          new ActionRowBuilder().addComponents(new ButtonBuilder().setLabel('Song').setURL(track.uri!).setStyle(ButtonStyle.Link)).toJSON(),
        ],
        flags: MessageFlags.IsComponentsV2,
        files: [file],
      });
    } else {
      const file = await generatePlaylist(searchResult);
      player.queue.add(searchResult.tracks);
      if (!player.playing) player.play();
      await interaction.followUp({
        files: [file],
        components: [new ActionRowBuilder().addComponents(new ButtonBuilder().setLabel('Playlist').setURL(query).setStyle(ButtonStyle.Link)).toJSON()],
      });
    }
  }
}
