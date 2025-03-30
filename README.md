<h1 align="center">Rhea</h1>

<h4 align="center">An easy to use, no ads music bot.</h4>

<p align="center">
  <a href="https://discord.gg/66dp9gxMZx">
    <img src="https://discordapp.com/api/guilds/918704583717572639/widget.png?style=shield" alt="Discord Server">
  </a>
  <a href="https://github.com/Azorant/Rhea/actions">
    <img src="https://img.shields.io/github/actions/workflow/status/Azorant/Rhea/docker-publish.yml?label=Build" alt="GitHub Actions">
  </a>
</p>

# Getting Started
Install [Docker](https://docs.docker.com/engine/install/) and [Docker Compose](https://docs.docker.com/compose/install/)

Create a `docker-compose.yml` with the following content:

```yaml
version: '3.8'

services:
  rhea:
    image: ghcr.io/azorant/rhea:latest
    container_name: rhea
    restart: unless-stopped
    environment:
      - DISCORD_INVITE=server invite
      - LAVALINK_NODE_1=Node1,http://lavalink:2333,youshallnotpass
      - TOKEN=bot token
      - GUILD_CHANNEL=channel ID for guild events
      - LOG_CHANNEL=channel for logging node and track events
    ports:
      - 3400:3400
  lavalink:
    image: ghcr.io/lavalink-devs/lavalink:latest
    container_name: lavalink
    restart: unless-stopped
    environment:
      - SERVER_PORT=2333
      - LAVALINK_SERVER_PASSWORD=youshallnotpass
    expose:
      - 2333
```

Run `docker compose up -d` to startup Rhea and Lavalink.

That's all! Now you're ready to listen to some music.

# Configuration

There are more config options available for Lavalink [here](https://lavalink.dev/configuration/).

If you'd wish to play Apple Music and/or Spotify links, then you'll have to setup the [LavaSrc](https://github.com/topi314/LavaSrc) plugin for Lavalink.
