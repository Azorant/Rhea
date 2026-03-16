import { ConsoleTransport, Logger } from 'acordia';
import 'dotenv/config';
import { CommandClient } from 'displosion';
import { Collection, GatewayIntentBits } from 'discord.js';
import path from 'path';
import { Library, Rainlink, type RainlinkNodeOptions } from 'rainlink';

export class Rhea {
  client = new CommandClient({
    context: this,
    commandsPath: path.join(import.meta.dirname, 'commands'),
    eventsPath: path.join(import.meta.dirname, 'events'),
    options: {
      intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildVoiceStates, GatewayIntentBits.GuildMessages],
    },
  });
  logger = Logger.createInstance('Rhea').addTransport(new ConsoleTransport());
  lavalink = new Rainlink({
    nodes: process.env.NODES!.split(',').map(this.parseNode),
    library: new Library.DiscordJS(this.client),
  });
  votes = new Collection<string, string[]>();

  constructor() {
    this.lavalink.on('nodeConnect', (node) => {
      this.logger.info(`Node ${node.options.name} connected`);
    });

    this.lavalink.on('nodeDisconnect', (node) => {
      this.logger.warning(`Node ${node.options.name} disconnected`);
    });

    this.lavalink.on('nodeError', (node, error) => {
      this.logger.error(`Node ${node.options.name} error:`, error);
    });

    this.lavalink.on('trackEnd', (player) => {
      this.votes.delete(player.voiceId!);
    });

    this.lavalink.on('playerWebsocketClosed', (player) => {
      player.destroy();
    });

    this.client.login(process.env.TOKEN);
  }

  parseNode(url: string): RainlinkNodeOptions {
    const node = new URL(url);
    return {
      host: node.hostname,
      auth: node.password,
      port: Number(node.port),
      secure: node.protocol === 'https',
      name: node.searchParams.get('identifier') || 'lavalink',
    };
  }
}

new Rhea();
