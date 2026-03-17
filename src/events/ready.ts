import { BaseEvent } from 'displosion';
import type { Rhea } from '../index.js';
import type { ClientEvents } from 'discord.js';

export default class ReadyEvent extends BaseEvent<Rhea> {
  event: keyof ClientEvents = 'ready';
  once = false;
  async execute(context: Rhea) {
    context.logger.success(`${context.client.user?.tag} ready`);
    await context.client.registerCommands(process.env.DEV_GUILD);
  }
}
