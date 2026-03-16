import { BaseEvent } from 'displosion';
import type { Rhea } from '../index.js';
import type { ClientEvents, VoiceBasedChannel, VoiceState } from 'discord.js';

export default class VoiceStateUpdateEvent extends BaseEvent<Rhea> {
  event: keyof ClientEvents = 'voiceStateUpdate';
  once = false;
  async execute(context: Rhea, oldState: VoiceState) {
    if (oldState.id === context.client.user?.id) return; // all voice events are handles by rainlink
    const player = context.lavalink.players.get(oldState.guild.id);
    if (!player) return;

    if (!oldState.channelId || oldState.channelId !== player.voiceId) return; // we only care about people leaving the vc
    const channel = (await oldState.guild.channels.fetch(oldState.channelId)) as VoiceBasedChannel | null;
    if (!channel) return;
    const users = channel.members.filter((m) => !m.user.bot);
    if (users.size) return;
    player.destroy();
  }
}
