using System.Collections.Immutable;
using Discord;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Rhea.Bot.Models;
using Rhea.Bot.Modules;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace Rhea.Bot.Services;

public static class ImageGenerator
{
    private static Font LargeFont { get; set; }
    private static Font SmallFont { get; set; }
    private static Font SmallerFont { get; set; }
    private static IReadOnlyList<FontFamily> FallbackFonts { get; set; }

    static ImageGenerator()
    {
        FontCollection collection = new();
        collection.Add("./Resources/Twemoji.Mozilla.ttf");
        collection.Add("./Resources/NotoSansSymbols-Regular.ttf");
        collection.Add("./Resources/GoNotoKurrent-Regular.ttf");
        FontFamily defaultFamily = collection.Add("./Resources/Roboto-Regular.ttf");
        LargeFont = defaultFamily.CreateFont(48, FontStyle.Regular);
        SmallFont = defaultFamily.CreateFont(32, FontStyle.Regular);
        SmallerFont = defaultFamily.CreateFont(28, FontStyle.Regular);
        FallbackFonts = new[]
        {
            collection.Get("Twemoji Mozilla"),
            collection.Get("Noto Sans Symbols"),
            collection.Get("Go Noto Kurrent-Regular")
        };
    }

    public static async Task<FileAttachment> GenerateSingle(TrackMetadata metadata)
    {
        using Image final = new Image<Bgra32>(1280, 720);

        var httpClient = new HttpClient();
        Image<Bgra32> artwork;

        if (!string.IsNullOrEmpty(metadata.ArtworkUri))
        {
            var httpResult = await httpClient.GetAsync(metadata.ArtworkUri);
            await using var resultStream = await httpResult.Content.ReadAsStreamAsync();
            artwork = (await Image.LoadAsync(resultStream)).CloneAs<Bgra32>();
            var ratio = (float)artwork.Width / artwork.Height;
            if (Math.Abs(ratio - 1f) > 0)
            {
                var smallestSize = artwork.Height > artwork.Width ? artwork.Width : artwork.Height;
                int xPos = artwork.Width / 2 - smallestSize / 2;
                int yPos = artwork.Height / 2 - smallestSize / 2;
                artwork.Mutate(x => x.Crop(new Rectangle(xPos, yPos, smallestSize, smallestSize)));
            }
        }
        else
        {
            artwork = Image.Load<Bgra32>("./Resources/placeholder.png");
        }

        var bg = GetDominateColor(artwork);
        final.Mutate(x => x.Fill(bg).ApplyRoundedCorners(20));
        var bgRgb = bg.ToPixel<Bgra32>();
        var isLight = Math.Sqrt(0.299 * (bgRgb.R * bgRgb.R) + 0.587 * (bgRgb.G * bgRgb.G) + 0.114 * (bgRgb.B * bgRgb.B)) > 127.5;

        var primaryColor = isLight ? Color.Black : Color.White;
        var secondaryColor = isLight ? Color.ParseHex("424242") : Color.LightGray;

        var artworkBoxLayer1 = new Image<Bgra32>(640, 640);
        artworkBoxLayer1.Mutate(x => x.Fill(Brushes.Solid(Color.LightGray)).ApplyRoundedCorners(20));
        var artworkBoxLayer2 = new Image<Bgra32>(635, 635);
        artworkBoxLayer2.Mutate(x => x.Fill(Brushes.Solid(Color.Gray)).ApplyRoundedCorners(20).Transform(new AffineTransformBuilder().AppendTranslation(new PointF(2.5f, 2.5f))));
        artworkBoxLayer1.Mutate(x => x.DrawImage(artworkBoxLayer2, 1).Transform(new AffineTransformBuilder().AppendTranslation(new PointF(40, 40))));

        final.Mutate(x => x.DrawImage(artworkBoxLayer1, 0.75f));

        artwork.Mutate(x => x.Resize(new Size(636)).ApplyRoundedCorners(20));
        final.Mutate(x => x.DrawImage(artwork, new Point(42, 42), 1));


        var baseHeight = 60;
        var titleOptions = new RichTextOptions(LargeFont)
        {
            FallbackFontFamilies = FallbackFonts,
            Origin = new PointF(720, baseHeight),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            WrappingLength = 512
        };
        var titleMeasure = TextMeasurer.MeasureSize(metadata.Title, titleOptions);
        final.Mutate(x => x.DrawText(titleOptions, metadata.Title, primaryColor));

        var artistOptions = new RichTextOptions(SmallFont)
        {
            FallbackFontFamilies = FallbackFonts,
            Origin = new PointF(720, titleOptions.Origin.Y + titleMeasure.Height + 20),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            WrappingLength = 512
        };
        var artistHeight = TextMeasurer.MeasureSize(metadata.Artist, artistOptions);
        final.Mutate(x => x.DrawText(artistOptions, metadata.Artist, secondaryColor));

        string duration = metadata.Livestream
            ? "Livestream"
            : metadata.CurrentPosition == null
                ? BaseModule.FormatTime(metadata.Duration)
                : $"{BaseModule.FormatTime((TimeSpan)metadata.CurrentPosition)} / {BaseModule.FormatTime(metadata.Duration)}";
        var durationOptions = new RichTextOptions(SmallFont)
        {
            Origin = new PointF(720, artistOptions.Origin.Y + artistHeight.Height + (metadata.CurrentPosition == null ? 20 : 80)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            WrappingLength = 512
        };
        final.Mutate(x => x.DrawText(durationOptions, duration, secondaryColor));

        if (metadata.CurrentPosition != null)
        {
            var position = (int)Math.Floor((decimal)metadata.CurrentPosition.Value.Ticks / metadata.Duration.Ticks * 100);
            final.Mutate(x => x.Draw(Brushes.Solid(primaryColor), 5, new Rectangle(720, (int)(durationOptions.Origin.Y - 20), 520, 1)));
            final.Mutate(x => x.Fill(Brushes.Solid(primaryColor), new EllipsePolygon((float)(720 + position * 5.2), durationOptions.Origin.Y - 19, 10)));
        }


        // These will always be null or not null together
        var hasInfo = metadata is { QueuePosition: not null, TimeToPlay: not null };

        if (hasInfo)
        {
            final.Mutate(x => x.DrawText("Position in queue", SmallerFont, secondaryColor, new PointF(720, 600)));
            final.Mutate(x => x.DrawText(metadata.QueuePosition.ToString()!, SmallerFont, secondaryColor, new PointF(720, 640)));

            final.Mutate(x => x.DrawText(new RichTextOptions(SmallerFont)
            {
                Origin = new PointF(1240, 600),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                WrappingLength = 512
            }, "Time until playing", secondaryColor));
            final.Mutate(x => x.DrawText(new RichTextOptions(SmallerFont)
            {
                Origin = new PointF(1240, 640),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                WrappingLength = 512
            }, BaseModule.FormatTime((TimeSpan)metadata.TimeToPlay!), secondaryColor));
        }


        if (!string.IsNullOrEmpty(metadata.Requester))
        {
            var requesterOptions = new RichTextOptions(SmallerFont)
            {
                FallbackFontFamilies = FallbackFonts,
                Origin = new PointF(720, hasInfo ? 590 : 660),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                WrappingLength = 512
            };
            final.Mutate(x => x.DrawText(requesterOptions, $"Requested by {metadata.Requester}", secondaryColor));
        }

        if (DiscordClientHost.IsDebug()) await final.SaveAsPngAsync("../../../tmp.png");
        Stream stream = new MemoryStream();
        await final.SaveAsync(stream, PngFormat.Instance);
        return new FileAttachment(stream, "cover.png");
    }

    public static async Task<FileAttachment> GeneratePlaylist(PlaylistInformation playlist, ImmutableArray<LavalinkTrack> tracks)
    {
        using Image final = new Image<Bgra32>(1280, 720);
        var random = new Random();
        var bg = Color.ParseHex($"{random.Next(0x1000000):X6}");
        final.Mutate(x => x.Fill(bg).ApplyRoundedCorners(20));

        var bgRgb = bg.ToPixel<Bgra32>();
        var isLight = Math.Sqrt(0.299 * (bgRgb.R * bgRgb.R) + 0.587 * (bgRgb.G * bgRgb.G) + 0.114 * (bgRgb.B * bgRgb.B)) > 127.5;

        var primaryColor = isLight ? Color.Black : Color.White;
        var secondaryColor = isLight ? Color.ParseHex("424242") : Color.LightGray;


        var baseHeight = 40;
        var nameOptions = new RichTextOptions(LargeFont)
        {
            FallbackFontFamilies = FallbackFonts,
            Origin = new PointF(40, baseHeight),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            WrappingLength = 1200
        };
        var nameMeasure = TextMeasurer.MeasureSize(playlist.Name, nameOptions);
        final.Mutate(x => x.DrawText(nameOptions, playlist.Name, primaryColor));

        var metadataOptions = new RichTextOptions(SmallerFont)
        {
            FallbackFontFamilies = FallbackFonts,
            Origin = new PointF(40, nameOptions.Origin.Y + nameMeasure.Height + 20),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            WrappingLength = 1200
        };
        var metadata = $"{tracks.Count()} tracks       {BaseModule.FormatTime(new TimeSpan(tracks.Sum(t => t.Duration.Ticks)))} length";
        var metadataMeasure = TextMeasurer.MeasureSize(metadata, metadataOptions);
        final.Mutate(x => x.DrawText(metadataOptions, metadata, secondaryColor));

        var offset = metadataOptions.Origin.Y + metadataMeasure.Height + 20;

        var slice = tracks.Take(8).ToList();
        var longest = slice.OrderByDescending(t => t.Duration).First();

        var durationMeasure = TextMeasurer.MeasureSize(BaseModule.FormatTime(longest.Duration), new RichTextOptions(SmallerFont)
        {
            Origin = new PointF(40, offset),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            WrappingLength = 1200
        });

        foreach (var track in slice)
        {
            var durationOptions = new RichTextOptions(SmallerFont)
            {
                Origin = new PointF(40 + durationMeasure.Width, offset),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                WrappingLength = 1200
            };
            var duration = BaseModule.FormatTime(track.Duration);
            final.Mutate(x => x.DrawText(durationOptions, duration, secondaryColor));

            var titleOptions = new RichTextOptions(SmallerFont)
            {
                FallbackFontFamilies = FallbackFonts,
                Origin = new PointF(40 + durationMeasure.Width + 20, offset),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                WrappingLength = 1200 - durationMeasure.Width - 20
            };

            var titleMeasure = TextMeasurer.MeasureSize(track.Title, titleOptions);
            final.Mutate(x => x.DrawText(titleOptions, track.Title, secondaryColor));
            offset += titleMeasure.Height + 10;
        }

        if (tracks.Count() > 8)
        {
            var moreOptions = new RichTextOptions(SmallerFont)
            {
                Origin = new PointF(40 + durationMeasure.Width + 20, offset),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                WrappingLength = 1200 - durationMeasure.Width - 20
            };
            final.Mutate(x => x.DrawText(moreOptions, $"+ {tracks.Count() - 8} more tracks", secondaryColor));
        }

        if (DiscordClientHost.IsDebug()) await final.SaveAsPngAsync("../../../tmp.png");
        Stream stream = new MemoryStream();
        await final.SaveAsync(stream, PngFormat.Instance);
        return new FileAttachment(stream, "playlist.png");
    }

    private static IImageProcessingContext ApplyRoundedCorners(this IImageProcessingContext context, float cornerRadius)
    {
        Size size = context.GetCurrentSize();
        IPathCollection corners = BuildCorners(size.Width, size.Height, cornerRadius);

        context.SetGraphicsOptions(new GraphicsOptions()
        {
            Antialias = true,

            AlphaCompositionMode = PixelAlphaCompositionMode.DestOut
        });

        foreach (IPath path in corners)
        {
            context = context.Fill(Color.Yellow, path);
        }

        return context;
    }

    private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
    {
        var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

        IPath cornerTopLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

        float rightPos = imageWidth - cornerTopLeft.Bounds.Width + 1;
        float bottomPos = imageHeight - cornerTopLeft.Bounds.Height + 1;

        IPath cornerTopRight = cornerTopLeft.RotateDegree(90).Translate(rightPos, 0);
        IPath cornerBottomLeft = cornerTopLeft.RotateDegree(-90).Translate(0, bottomPos);
        IPath cornerBottomRight = cornerTopLeft.RotateDegree(180).Translate(rightPos, bottomPos);

        return new PathCollection(cornerTopLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
    }

    private static Color GetDominateColor(Image<Bgra32> image)
    {
        int r = 0;
        int g = 0;
        int b = 0;
        int totalPixels = 0;

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var pixel = image[x, y];

                r += Convert.ToInt32(pixel.R);
                g += Convert.ToInt32(pixel.G);
                b += Convert.ToInt32(pixel.B);

                totalPixels++;
            }
        }

        if (totalPixels == 0) throw new ArithmeticException("Total pixels is 0");

        r /= totalPixels;
        g /= totalPixels;
        b /= totalPixels;

        return Color.FromRgb((byte)r, (byte)g, (byte)b);
    }
}