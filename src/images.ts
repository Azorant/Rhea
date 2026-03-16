import { createCanvas, GlobalFonts, Image, loadImage, type CanvasTextAlign, type CanvasTextBaseline, type SKRSContext2D } from '@napi-rs/canvas';
import { Vibrant } from 'node-vibrant/node';
import { writeFileSync } from 'fs';
import type { RainlinkSearchResult, RainlinkTrack } from 'rainlink';
import { join } from 'path';
import { AttachmentBuilder } from 'discord.js';
import { toTimestamp } from '@azorant/time';

GlobalFonts.registerFromPath(join(import.meta.dirname, 'resources', 'GoNotoKurrent-Regular.ttf'));
GlobalFonts.registerFromPath(join(import.meta.dirname, 'resources', 'NotoSansSymbols-Regular.ttf'));
GlobalFonts.registerFromPath(join(import.meta.dirname, 'resources', 'Roboto-Regular.ttf'));
GlobalFonts.registerFromPath(join(import.meta.dirname, 'resources', 'Twemoji-Regular.ttf'));

enum FontSize {
  Large = 40,
  Small = 32,
  Tiny = 28,
  Huge = 64,
}

const placeholder = await loadImage(join(import.meta.dirname, 'resources', 'placeholder.png'));

export async function generateSingle(track: RainlinkTrack, queueInfo?: { position: number; duration: number }) {
  const canvas = createCanvas(1280, 720);
  const ctx = canvas.getContext('2d', { alpha: true });

  let artwork: Image | null = null;
  if (track.artworkUrl?.length) {
    try {
      artwork = await loadImage(track.artworkUrl);
      // eslint-disable-next-line @typescript-eslint/no-unused-vars, no-empty
    } catch (_) {}
  }

  const artworkPalette = artwork ? await Vibrant.from(Buffer.from(artwork.src)).getPalette() : null;

  const isLight = artworkPalette?.Vibrant
    ? Math.sqrt(
        0.299 * (artworkPalette?.Vibrant.r * artworkPalette?.Vibrant.r) +
          0.587 * (artworkPalette?.Vibrant.g * artworkPalette?.Vibrant.g) +
          0.114 * (artworkPalette?.Vibrant.b * artworkPalette?.Vibrant.b),
      ) > 127.5
    : false;

  const primaryColour = isLight ? '#000' : '#fff';
  const secondaryColour = isLight ? '#424242' : '#616161';

  drawRect({ ctx, colour: artworkPalette?.Vibrant?.hex ?? '#000', x: 0, y: 0, w: 1280, h: 720, r: 20 }); // background
  drawRect({ ctx, colour: primaryColour, x: 40, y: 40, w: 640, h: 640, r: 20, alpha: 0.75 }); // artwork background
  drawRect({ ctx, colour: secondaryColour, x: 42, y: 42, w: 636, h: 636, r: 20, alpha: 0.75 }); // artwork foreground

  drawImage({ ctx, image: artwork ?? placeholder, x: 42, y: 42, dx: 636, dy: 636, r: 20 }); // draw artwork

  let baseHeight = drawText({ ctx, fillStyle: primaryColour, text: track.title, x: 720, y: 60, maxWidth: 520 }); // title
  baseHeight = drawText({ ctx, fillStyle: primaryColour, text: track.author, x: 720, y: baseHeight + 20, maxWidth: 520, fontSize: FontSize.Small }); // author/artist
  if (track.position) {
    const position = Math.floor((track.position / track.duration) * 100);
    drawRect({ ctx, colour: primaryColour, x: 720, y: baseHeight + 40, w: 520, h: 5, r: 5 });
    drawRect({ ctx, colour: primaryColour, x: 720 + position * 5.2, y: baseHeight + 32.5, w: 20, h: 20, r: 20 });
  }
  drawText({
    ctx,
    fillStyle: primaryColour,
    text: track.isStream ? 'Livestream' : `${track.position ? `${toTimestamp(track.position)} / ` : ''}${toTimestamp(track.duration)}`,
    x: 720,
    y: baseHeight + (track.position ? 80 : 20),
    maxWidth: 520,
    fontSize: FontSize.Small,
  }); // position/duration

  if (queueInfo) {
    drawText({ ctx, fontSize: FontSize.Tiny, fillStyle: primaryColour, text: 'Position in queue', x: 720, y: 600, maxWidth: 520 });
    drawText({ ctx, fontSize: FontSize.Tiny, fillStyle: primaryColour, text: queueInfo.position.toString(), x: 720, y: 640, maxWidth: 520 });
    drawText({ ctx, fontSize: FontSize.Tiny, fillStyle: primaryColour, text: 'Time until playing', x: 1240, y: 600, maxWidth: 520, align: 'right', baseline: 'top' });
    drawText({ ctx, fontSize: FontSize.Tiny, fillStyle: primaryColour, text: toTimestamp(queueInfo.duration), x: 1240, y: 640, maxWidth: 520, align: 'right', baseline: 'top' });
  }

  drawText({
    ctx,
    fontSize: FontSize.Tiny,
    fillStyle: primaryColour,
    text: `Requested by ${track.requester}`,
    x: 720,
    y: queueInfo ? 560 : 640,
    maxWidth: 520,
  });

  if (process.env.DEBUG === 'true') writeFileSync('./image.png', canvas.toBuffer('image/png'));
  return new AttachmentBuilder(canvas.encodeSync('png'), { name: 'image.png' });
}

export async function generateQueue(queue: RainlinkTrack[]) {
  const canvas = createCanvas(1920, 1080);
  const ctx = canvas.getContext('2d', { alpha: true });
  drawRect({ ctx, colour: '#65566E', x: 0, y: 0, w: 1920, h: 1080, r: 20 }); // background

  let y = 48;
  const x = 48;
  const artworkSize = 96;
  const padding = 40;
  const timestampWidth = 160;

  for (let i = 0; i < queue.length; i++) {
    const track = queue[i]!;
    let artwork: Image;
    if (track.artworkUrl?.length) {
      try {
        artwork = await loadImage(track.artworkUrl);
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
      } catch (_) {
        artwork = placeholder;
      }
    } else artwork = placeholder;

    drawText({
      ctx,
      fontSize: FontSize.Huge,
      fillStyle: 'white',
      text: `${i + 1}.`,
      x: x + padding / 2,
      y,
      maxWidth: padding * 2,
      align: 'center',
    });
    const artworkX = x + padding * 2;
    drawImage({ ctx, image: artwork, x: artworkX, y: y - artworkSize / 5, dx: artworkSize, dy: artworkSize, r: 20 });
    const timestampX = artworkX + artworkSize + padding + timestampWidth;
    const timestampHeight = drawText({
      ctx,
      fontSize: FontSize.Huge,
      fillStyle: 'white',
      text: track.isStream ? 'Stream' : toTimestamp(track.duration),
      x: timestampX - timestampWidth / 2,
      y,
      maxWidth: timestampWidth,
      align: 'center',
    });
    const titleHeight = drawText({ ctx, fontSize: FontSize.Huge, fillStyle: 'white', text: track.title, x: timestampX + padding, y, maxWidth: 1920 - timestampX - padding * 2 });
    y = Math.max(timestampHeight, titleHeight) + padding;
    if (y + padding >= 1080) break;
  }
  if (process.env.DEBUG === 'true') writeFileSync('./image.png', canvas.toBuffer('image/png'));
  return new AttachmentBuilder(canvas.encodeSync('png'), { name: 'image.png' });
}

export async function generatePlaylist(search: RainlinkSearchResult) {
  const canvas = createCanvas(1920, 1080);
  const ctx = canvas.getContext('2d', { alpha: true });
  drawRect({ ctx, colour: '#65566E', x: 0, y: 0, w: 1920, h: 1080, r: 20 }); // background
  const padding = 40;
  const x = 48;
  let y = 48;
  const titleHeight = drawText({ ctx, fontSize: FontSize.Huge, fillStyle: 'white', text: search.playlistName ?? 'Unnamed Playlist', x, y: y, maxWidth: 1920 - padding * 2 });
  const metaHeight = drawText({
    ctx,
    fontSize: FontSize.Large,
    fillStyle: 'white',
    text: `${search.tracks.length} tracks    ${toTimestamp(search.tracks.reduce((a, b) => a + b.duration, 0))}`,
    x,
    y: titleHeight + padding / 2,
    maxWidth: 1920 - padding * 2,
  });

  const artworkSize = 96;
  const timestampWidth = 160;
  y = metaHeight + padding;

  for (let i = 0; i < search.tracks.length; i++) {
    const track = search.tracks[i]!;
    let artwork: Image;
    if (track.artworkUrl?.length) {
      try {
        artwork = await loadImage(track.artworkUrl);
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
      } catch (_) {
        artwork = placeholder;
      }
    } else artwork = placeholder;

    const artworkX = x;
    drawImage({ ctx, image: artwork, x: artworkX, y: y - artworkSize / 5, dx: artworkSize, dy: artworkSize, r: 20 });
    const timestampX = artworkX + artworkSize + padding + timestampWidth;
    const timestampHeight = drawText({
      ctx,
      fontSize: FontSize.Huge,
      fillStyle: 'white',
      text: track.isStream ? 'Stream' : toTimestamp(track.duration),
      x: timestampX - timestampWidth / 2,
      y,
      maxWidth: timestampWidth,
      align: 'center',
    });
    const titleHeight = drawText({ ctx, fontSize: FontSize.Huge, fillStyle: 'white', text: track.title, x: timestampX + padding, y, maxWidth: 1920 - timestampX - padding * 2 });
    y = Math.max(timestampHeight, titleHeight) + padding;
    if (y + padding >= 1080) break;
  }
  if (process.env.DEBUG === 'true') writeFileSync('./image.png', canvas.toBuffer('image/png'));
  return new AttachmentBuilder(canvas.encodeSync('png'), { name: 'image.png' });
}

function drawRect({ ctx, colour, x, y, w, h, r, alpha = 1 }: { ctx: SKRSContext2D; colour: string; x: number; y: number; w: number; h: number; r: number; alpha?: number }) {
  ctx.save();
  ctx.globalAlpha = alpha;
  ctx.fillStyle = colour;
  ctx.beginPath();
  ctx.roundRect(x, y, w, h, r);
  ctx.fill();
  ctx.restore();
}

function drawImage({ ctx, image, x, y, dx, dy, r }: { ctx: SKRSContext2D; image: Image; x: number; y: number; dx: number; dy: number; r: number }) {
  const ratio = image.width / image.height;
  const targetRatio = 1;

  let sx, sy, sWidth, sHeight;

  if (ratio > targetRatio) {
    // Image is wider than square - crop left/right
    sHeight = image.height;
    sWidth = image.height * targetRatio;
    sx = (image.width - sWidth) / 2;
    sy = 0;
  } else {
    // Image is taller than square - crop top/bottom
    sWidth = image.width;
    sHeight = image.width / targetRatio;
    sx = 0;
    sy = (image.height - sHeight) / 2;
  }

  ctx.save();
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'high';
  ctx.beginPath();
  ctx.roundRect(x, y, dx, dy, r);
  ctx.clip();
  ctx.drawImage(image, sx, sy, sWidth, sHeight, x, y, dx, dy);
  ctx.restore();
}

function drawText({
  ctx,
  text,
  x,
  y,
  maxWidth,
  fontSize = FontSize.Large,
  fillStyle = '#000',
  align = 'left',
  baseline = 'top',
  lineHeight = 1.2,
}: {
  ctx: SKRSContext2D;
  text: string;
  x: number;
  y: number;
  maxWidth?: number;
  fontSize?: number;
  fillStyle?: string;
  align?: CanvasTextAlign;
  baseline?: CanvasTextBaseline;
  lineHeight?: number;
}) {
  ctx.save();
  ctx.font = `${fontSize}px Roboto, Twemoji Mozilla, Noto Sans Symbols, Go Noto Kurrent-Regular`;
  ctx.fillStyle = fillStyle;
  ctx.textAlign = align;
  ctx.textBaseline = baseline;

  const words = text.split(' ');
  const lines: string[] = [];
  let current = '';

  for (const word of words) {
    const test = current ? `${current} ${word}` : word;
    const metrics = ctx.measureText(test);
    if (maxWidth && metrics.width > maxWidth) {
      if (current) lines.push(current);
      current = word;
    } else {
      current = test;
    }
  }
  if (current) lines.push(current);

  const size = fontSize;
  const step = size * lineHeight;

  for (let i = 0; i < lines.length; i++) {
    ctx.fillText(lines[i]!, x, y + i * step, maxWidth);
  }

  ctx.restore();
  return y + lines.length * step;
}
