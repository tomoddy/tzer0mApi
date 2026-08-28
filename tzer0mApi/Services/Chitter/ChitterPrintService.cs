using SkiaSharp;
using System.Net.Sockets;

namespace tzer0mApi.Services.Chitter;

/// <summary>
/// Renders receipt content (body text plus a footer) to a monochrome bitmap and sends it to the configured network receipt printer as an ESC/POS raster image.
/// </summary>
/// <param name="config">The configuration instance.</param>
/// <param name="env">The web host environment, used to resolve the bundled font file's path.</param>
/// <param name="logger">The logger instance.</param>
public class ChitterPrintService(IConfiguration config, IWebHostEnvironment env, ILogger<ChitterPrintService> logger)
{
    /// <summary>
    /// The printer's IP address.
    /// </summary>
    private readonly string PrinterIp = config["Chitter:Printer:Ip"] ?? throw new InvalidOperationException("Chitter:Printer:Ip is not configured");

    /// <summary>
    /// The printer's raw/JetDirect port.
    /// </summary>
    private readonly int PrinterPort = config.GetValue<int?>("Chitter:Printer:Port") ?? 9100;

    /// <summary>
    /// Leading feed, in lines, sent before content prints.
    /// </summary>
    private readonly int FeedBefore = config.GetValue<int?>("Chitter:Printer:FeedBefore") ?? 0;

    /// <summary>
    /// Trailing feed, in lines, sent after content prints and before the cut.
    /// </summary>
    private readonly int FeedAfter = config.GetValue<int?>("Chitter:Printer:FeedAfter") ?? 4;

    /// <summary>
    /// The printable image width in dots, matching the printer's confirmed usable width.
    /// </summary>
    private readonly int WidthPx = config.GetValue<int?>("Chitter:Image:WidthPx") ?? 512;

    /// <summary>
    /// Font size, in points, used for the receipt body.
    /// </summary>
    private readonly float BodyFontSize = config.GetValue<float?>("Chitter:Image:BodyFontSize") ?? 28f;

    /// <summary>
    /// Font size, in points, used for the footer.
    /// </summary>
    private readonly float FooterFontSize = config.GetValue<float?>("Chitter:Image:FooterFontSize") ?? 18f;

    /// <summary>
    /// Margin, in pixels, applied around all rendered content.
    /// </summary>
    private readonly int MarginPx = config.GetValue<int?>("Chitter:Image:MarginPx") ?? 12;

    /// <summary>
    /// Path, relative to the content root, of the bold font used for section headings, marked in body text as **like this**.
    /// </summary>
    private readonly string BoldFontRelativePath = config["Chitter:Image:BoldFontPath"] ?? "Assets/Fonts/SpaceGrotesk-Bold.ttf";

    /// <summary>
    /// Path, relative to the content root, of the monochrome emoji font used as a fallback for characters Space Grotesk doesn't cover.
    /// </summary>
    private readonly string EmojiFontRelativePath = config["Chitter:Image:EmojiFontPath"] ?? "Assets/Fonts/NotoEmoji-Medium.ttf";

    /// <summary>
    /// Path, relative to the content root, of the CJK font used as a fallback for Chinese characters Space Grotesk doesn't cover.
    /// </summary>
    private readonly string CjkFontRelativePath = config["Chitter:Image:CjkFontPath"] ?? "Assets/Fonts/NotoSansSC-Medium.ttf";

    /// <summary>
    /// Maximum height, in pixels, of a single raster image command.
    /// </summary>
    private readonly int MaxBandHeightPx = config.GetValue<int?>("Chitter:Image:MaxBandHeightPx") ?? 48;

    /// <summary>
    /// Maximum width, in pixels, an uploaded photo is allowed to print at - null (the default) means the full printable width, since ImageDensityMode keeps the transmitted data small without needing to shrink the physical print.
    /// </summary>
    private readonly int? MaxImageWidthPx = config.GetValue<int?>("Chitter:Image:MaxImageWidthPx");

    /// <summary>
    /// Maximum height, in pixels, an uploaded photo is allowed to print at - a tall photo is centre-cropped down to this rather than sent at full height.
    /// </summary>
    private readonly int MaxImageHeightPx = config.GetValue<int?>("Chitter:Image:MaxImageHeightPx") ?? 480;

    /// <summary>
    /// Delay, in milliseconds, after each image band is flushed to the printer, giving it time to physically print and drain its receive buffer before the next band arrives.
    /// </summary>
    private readonly int BandDelayMs = config.GetValue<int?>("Chitter:Image:BandDelayMs") ?? 700;

    /// <summary>
    /// The density mode byte (m) sent in the GS v 0 raster header for a photo. 3 doubles both width and height on the printer side, so only a quarter of the pixel data needs to be transmitted for the same physical print size - 1 doubles width only, 2 doubles height only, 0 disables doubling and transmits at full resolution. Not every ESC/POS-compatible printer honours modes above 0, so this is safe to flip back to 0 via config alone if a photo prints garbled, with no redeploy needed.
    /// </summary>
    private readonly int ImageDensityMode = config.GetValue<int?>("Chitter:Image:DensityMode") ?? 3;

    /// <summary>
    /// Renders the given body text with a divider-and-timestamp footer beneath it, and sends the resulting image to the printer.
    /// </summary>
    /// <param name="bodyText">The text to print above the footer.</param>
    /// <returns>True if the payload was sent successfully, false otherwise.</returns>
    public async Task<bool> PrintTextAsync(string bodyText)
    {
        // Setup fonts and colours. Body/footer fall back from Space Grotesk, to the monochrome emoji font, to a CJK font.
        using SKTypeface typeface = LoadTypeface(config["Chitter:Image:FontPath"] ?? "Assets/Fonts/SpaceGrotesk-Medium.ttf");
        using SKTypeface boldTypeface = LoadTypeface(BoldFontRelativePath);
        using SKTypeface emojiTypeface = LoadTypeface(EmojiFontRelativePath);
        using SKTypeface cjkTypeface = LoadTypeface(CjkFontRelativePath);
        using SKFont bodyFont = new(typeface, BodyFontSize);
        using SKFont bodyBoldFont = new(boldTypeface, BodyFontSize);
        using SKFont bodyEmojiFont = new(emojiTypeface, BodyFontSize);
        using SKFont bodyCjkFont = new(cjkTypeface, BodyFontSize);
        using SKFont footerFont = new(typeface, FooterFontSize);
        using SKFont footerEmojiFont = new(emojiTypeface, FooterFontSize);
        using SKFont footerCjkFont = new(cjkTypeface, FooterFontSize);
        SKFont[] bodyFonts = [bodyFont, bodyEmojiFont, bodyCjkFont];
        SKFont[] bodyBoldFonts = [bodyBoldFont, bodyEmojiFont, bodyCjkFont];
        SKFont[] footerFonts = [footerFont, footerEmojiFont, footerCjkFont];
        using SKPaint paint = new() { Color = SKColors.Black, IsAntialias = true };

        // Calculate the usable width for text.
        int contentWidth = WidthPx - (MarginPx * 2);

        // Wrap the body text - a line wrapped in **like this** prints bold and unmarked, for section headings.
        List<(string Text, bool IsBold)> bodyLines = WrapBodyText(bodyText, bodyFonts, bodyBoldFonts, paint, contentWidth);

        // Work out the body and footer block heights.
        float bodyLineHeight = BodyFontSize * 1.3f;
        int bodyBlockHeight = (int)Math.Ceiling(bodyLines.Count * bodyLineHeight);
        int footerBlockHeight = FooterBlockHeight();
        int totalHeight = MarginPx + bodyBlockHeight + MarginPx + footerBlockHeight + MarginPx;

        // Create a bitmap and erase it to white.
        using SKBitmap bitmap = new(WidthPx, totalHeight);
        bitmap.Erase(SKColors.White);
        using (SKCanvas canvas = new(bitmap))
        {
            // Calculate the Y position for each line and draw the body.
            float y = MarginPx + BodyFontSize;
            foreach ((string line, bool isBold) in bodyLines)
            {
                DrawMixedText(canvas, line, MarginPx, y, isBold ? bodyBoldFonts : bodyFonts, paint);
                y += bodyLineHeight;
            }

            // Draw the footer divider and timestamp beneath the body.
            DrawFooter(canvas, footerFonts, paint, contentWidth, MarginPx + bodyBlockHeight + MarginPx);
        }

        // Convert the bitmap to a 1-bit monochrome raster image, split into paced bands, and send it to the printer.
        List<PrintSegment> segments = BuildImageSegments(bitmap);
        return await SendPacedAsync(segments);
    }

    /// <summary>
    /// Resizes the given image to the printer's usable content width, dithers it to 1-bit monochrome using Floyd-Steinberg error diffusion, and sends the result to the printer beneath the same margin used for text.
    /// </summary>
    /// <param name="imageStream">The image content stream, in any format SkiaSharp can decode.</param>
    /// <returns>True if the payload was sent successfully, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">The stream could not be decoded or resized as an image.</exception>
    public async Task<bool> PrintImageAsync(Stream imageStream)
    {
        // Decode the uploaded image.
        using SKBitmap original = SKBitmap.Decode(imageStream) ?? throw new InvalidOperationException("Could not decode image.");

        // Resize to MaxImageWidthPx (not the full printable width - see its doc comment), preserving aspect ratio.
        int contentWidth = WidthPx - (MarginPx * 2);
        int printedWidth = Math.Min(MaxImageWidthPx ?? contentWidth, contentWidth);
        int targetHeight = Math.Max(1, (int)Math.Round(original.Height * (printedWidth / (double)original.Width)));
        SKSamplingOptions sampling = new(SKFilterMode.Linear, SKMipmapMode.None);
        using SKBitmap resized = original.Resize(new SKImageInfo(printedWidth, targetHeight), sampling) ?? throw new InvalidOperationException("Could not resize image.");

        // Cap the printed height, centre-cropping a taller image down rather than printing it at full height.
        int printedHeight = Math.Min(targetHeight, MaxImageHeightPx);
        int cropTop = (targetHeight - printedHeight) / 2;
        using SKBitmap cropped = new(printedWidth, printedHeight);
        using (SKCanvas cropCanvas = new(cropped))
            cropCanvas.DrawBitmap(resized, new SKRect(0, cropTop, printedWidth, cropTop + printedHeight), new SKRect(0, 0, printedWidth, printedHeight), sampling);

        // Load the footer's typeface fallback chain, matching the text footer's styling.
        using SKTypeface typeface = LoadTypeface(config["Chitter:Image:FontPath"] ?? "Assets/Fonts/SpaceGrotesk-Medium.ttf");
        using SKTypeface emojiTypeface = LoadTypeface(EmojiFontRelativePath);
        using SKTypeface cjkTypeface = LoadTypeface(CjkFontRelativePath);
        using SKFont footerFont = new(typeface, FooterFontSize);
        using SKFont footerEmojiFont = new(emojiTypeface, FooterFontSize);
        using SKFont footerCjkFont = new(cjkTypeface, FooterFontSize);
        SKFont[] footerFonts = [footerFont, footerEmojiFont, footerCjkFont];
        using SKPaint paint = new() { Color = SKColors.Black, IsAntialias = true };

        // Compose the photo, plus its top/bottom margin, onto a full-width canvas at full resolution, centring it horizontally.
        int regionHeight = MarginPx + printedHeight + MarginPx;
        using SKBitmap region = new(WidthPx, regionHeight);
        region.Erase(SKColors.White);
        using (SKCanvas regionCanvas = new(region))
        {
            float imageX = MarginPx + ((contentWidth - printedWidth) / 2f);
            regionCanvas.DrawBitmap(cropped, imageX, MarginPx, sampling);
        }

        // Downscale the whole region by ImageDensityMode's doubling factor before dithering, so the printer can double it back to the same physical size while only receiving a quarter (or half, for the width/height-only modes) of the pixel data. DensityMode 0 skips this - scale stays 1, and the full-resolution region is transmitted as-is.
        int scale = ImageDensityMode == 0 ? 1 : 2;
        int transmitWidth = Math.Max(1, WidthPx / scale);
        int transmitHeight = Math.Max(1, regionHeight / scale);
        using SKBitmap transmitRegion = region.Resize(new SKImageInfo(transmitWidth, transmitHeight), sampling) ?? throw new InvalidOperationException("Could not resize image region for transmission.");
        using SKBitmap ditheredRegion = DitherToMonochrome(transmitRegion);

        // Build the footer separately, at full resolution and normal density (m=0) - it's text, so it isn't a candidate for the lossy downscale above.
        int footerBlockHeight = FooterBlockHeight();
        using SKBitmap footerBitmap = new(WidthPx, footerBlockHeight);
        footerBitmap.Erase(SKColors.White);
        using (SKCanvas footerCanvas = new(footerBitmap))
            DrawFooter(footerCanvas, footerFonts, paint, contentWidth, 0);

        // Send the footer's raster command first, then the photo's - each raster is packed 180-degree-rotated internally (see BuildRasterCommand/rotatedX/rotatedY), which makes the printer's overall visual order the reverse of transmission order, so sending the photo last is what puts it on top with the footer beneath it. The divider line already visually separates the two, so there's no seam to see at that boundary.
        List<PrintSegment> segments =
        [
            new PrintSegment([0x1B, 0x40, 0x1B, 0x64, (byte)FeedBefore], BandDelayMs),
            new PrintSegment(BuildRasterCommand(footerBitmap, 0), BandDelayMs),
            new PrintSegment(BuildRasterCommand(ditheredRegion, (byte)ImageDensityMode), BandDelayMs),
            new PrintSegment([0x1B, 0x64, (byte)FeedAfter, 0x1D, 0x56, 0x00], 0),
        ];
        return await SendPacedAsync(segments);
    }

    /// <summary>
    /// Converts the given bitmap to pure black/white pixels using Floyd-Steinberg error diffusion.
    /// </summary>
    /// <param name="source">The bitmap to dither.</param>
    private static SKBitmap DitherToMonochrome(SKBitmap source)
    {
        // Get the dimensions of the source bitmap.
        int width = source.Width;
        int height = source.Height;

        // Work in a floating-point luminance buffer so diffused error can push values outside 0-255 temporarily.
        float[,] luminance = new float[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor pixel = source.GetPixel(x, y);
                luminance[x, y] = (pixel.Red * 0.299f) + (pixel.Green * 0.587f) + (pixel.Blue * 0.114f);
            }
        }

        // Threshold each pixel in turn, diffusing its rounding error onto its not-yet-visited neighbours.
        SKBitmap result = new(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBlack = luminance[x, y] < 128f;
                result.SetPixel(x, y, isBlack ? SKColors.Black : SKColors.White);
                float error = luminance[x, y] - (isBlack ? 0f : 255f);
                if (x + 1 < width)
                    luminance[x + 1, y] += error * 7f / 16f;
                if (y + 1 < height)
                {
                    if (x - 1 >= 0)
                        luminance[x - 1, y + 1] += error * 3f / 16f;
                    luminance[x, y + 1] += error * 5f / 16f;
                    if (x + 1 < width)
                        luminance[x + 1, y + 1] += error * 1f / 16f;
                }
            }
        }

        // Return the dithered monochrome bitmap.
        return result;
    }

    /// <summary>
    /// Height, in pixels, of the divider line drawn above the footer timestamp.
    /// </summary>
    private const int FooterDividerHeight = 24;

    /// <summary>
    /// The total height, in pixels, of the footer block (divider plus timestamp line).
    /// </summary>
    private int FooterBlockHeight() => FooterDividerHeight + (int)Math.Ceiling(FooterFontSize * 1.3f);

    /// <summary>
    /// Draws the footer's divider line and timestamp, anchored at the given top y position.
    /// </summary>
    /// <param name="canvas">The canvas to draw onto.</param>
    /// <param name="footerFonts">The fallback chain used for the timestamp text, in priority order.</param>
    /// <param name="paint">The paint used to draw.</param>
    /// <param name="contentWidth">The usable content width, in pixels.</param>
    /// <param name="topY">The y position the footer block starts at.</param>
    private void DrawFooter(SKCanvas canvas, SKFont[] footerFonts, SKPaint paint, int contentWidth, int topY)
    {
        // Draw the divider as an actual line spanning the full content width.
        float dividerY = topY + (FooterDividerHeight / 2f);
        canvas.DrawLine(MarginPx, dividerY, MarginPx + contentWidth, dividerY, paint);

        // Draw the timestamp beneath it.
        float timestampY = topY + FooterDividerHeight + FooterFontSize;
        string footerTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        DrawMixedText(canvas, footerTimestamp, MarginPx, timestampY, footerFonts, paint);
    }

    /// <summary>
    /// Loads a bundled typeface from disk.
    /// </summary>
    /// <param name="fontRelativePath">The font file's path, relative to the content root.</param>
    private SKTypeface LoadTypeface(string fontRelativePath)
    {
        string fontPath = Path.Combine(env.ContentRootPath, fontRelativePath);
        return SKTypeface.FromFile(fontPath) ?? throw new InvalidOperationException($"Could not load font at {fontPath}");
    }

    /// <summary>
    /// Picks the first font in the given fallback chain whose typeface contains a glyph for the given rune, falling back to the first font in the chain.
    /// </summary>
    /// <param name="rune">The rune to find a font for.</param>
    /// <param name="fonts">The fallback chain, in priority order.</param>
    private static SKFont FontForRune(System.Text.Rune rune, SKFont[] fonts)
    {
        foreach (SKFont font in fonts)
        {
            if (font.ContainsGlyph(rune.Value))
                return font;
        }
        return fonts[0];
    }

    /// <summary>
    /// Splits the given text into runs of consecutive runes that resolve to the same font.
    /// </summary>
    /// <param name="text">The text to split into runs.</param>
    /// <param name="fonts">The fallback chain, in priority order.</param>
    private static List<(string Text, SKFont Font)> SplitIntoFontRuns(string text, SKFont[] fonts)
    {
        // Create a list of runs, each containing the text and the font used for that run.
        List<(string Text, SKFont Font)> runs = [];
        System.Text.StringBuilder currentRun = new();
        SKFont? currentFont = null;

        // Iterate through each rune in the text.
        foreach (System.Text.Rune rune in text.EnumerateRunes())
        {
            // Determine which font to use for this rune.
            SKFont font = FontForRune(rune, fonts);
            if (currentFont != null && font != currentFont)
            {
                runs.Add((currentRun.ToString(), currentFont));
                currentRun.Clear();
            }

            // Append the rune to the current run and update the current font.
            currentRun.Append(rune.ToString());
            currentFont = font;
        }

        // Add the last run if it exists.
        if (currentFont != null)
            runs.Add((currentRun.ToString(), currentFont));

        // Return the list of font runs.
        return runs;
    }

    /// <summary>
    /// Measures the width of the given text across mixed fonts, summing each font run's measured width.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="fonts">The fallback chain, in priority order.</param>
    /// <param name="paint">The paint used to measure text width.</param>
    private static float MeasureMixedText(string text, SKFont[] fonts, SKPaint paint)
    {
        float total = 0f;
        foreach ((string runText, SKFont runFont) in SplitIntoFontRuns(text, fonts))
            total += runFont.MeasureText(runText, paint);
        return total;
    }

    /// <summary>
    /// Draws the given text across mixed fonts, advancing the x position by each font run's measured width.
    /// </summary>
    /// <param name="canvas">The canvas to draw onto.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="x">The starting x position.</param>
    /// <param name="y">The baseline y position.</param>
    /// <param name="fonts">The fallback chain, in priority order.</param>
    /// <param name="paint">The paint used to draw and measure text.</param>
    private static void DrawMixedText(SKCanvas canvas, string text, float x, float y, SKFont[] fonts, SKPaint paint)
    {
        float currentX = x;
        foreach ((string runText, SKFont runFont) in SplitIntoFontRuns(text, fonts))
        {
            canvas.DrawText(runText, currentX, y, SKTextAlign.Left, runFont, paint);
            currentX += runFont.MeasureText(runText, paint);
        }
    }

    /// <summary>
    /// Splits the given text into lines that each fit within the given pixel width, wrapping on word boundaries. A paragraph wrapped in **like this** has the markers stripped and is wrapped/measured against boldFonts instead of fonts, with IsBold set on every line it produces.
    /// </summary>
    /// <param name="text">The text to wrap. Existing newlines are treated as forced line breaks.</param>
    /// <param name="fonts">The fallback chain used for normal paragraphs, in priority order.</param>
    /// <param name="boldFonts">The fallback chain used for **bold** paragraphs, in priority order.</param>
    /// <param name="paint">The paint used to measure text width.</param>
    /// <param name="maxWidth">The maximum line width, in pixels.</param>
    private static List<(string Text, bool IsBold)> WrapBodyText(string text, SKFont[] fonts, SKFont[] boldFonts, SKPaint paint, int maxWidth)
    {
        // Split the text into paragraphs and then wrap each paragraph into lines.
        List<(string Text, bool IsBold)> lines = [];
        foreach (string rawParagraph in text.Split('\n'))
        {
            // A paragraph wrapped in ** markers on both ends prints bold, with the markers themselves stripped.
            bool isBold = rawParagraph.Length > 4 && rawParagraph.StartsWith("**") && rawParagraph.EndsWith("**");
            string paragraph = isBold ? rawParagraph[2..^2] : rawParagraph;
            SKFont[] paragraphFonts = isBold ? boldFonts : fonts;

            // Split the paragraph into words, skip if the line has no words.
            string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add((string.Empty, isBold));
                continue;
            }

            // Build lines by adding words until the line exceeds the maximum width.
            string currentLine = words[0];
            for (int i = 1; i < words.Length; i++)
            {
                string candidate = currentLine + " " + words[i];
                if (MeasureMixedText(candidate, paragraphFonts, paint) <= maxWidth)
                {
                    currentLine = candidate;
                }
                else
                {
                    lines.Add((currentLine, isBold));
                    currentLine = words[i];
                }
            }

            // Add the last line of the paragraph.
            lines.Add((currentLine, isBold));
        }

        // Return the wrapped lines.
        return lines;
    }

    /// <summary>
    /// One ESC/POS byte segment to send to the printer, paired with how long to wait after sending it before the next segment goes out.
    /// </summary>
    /// <param name="Data">The raw bytes to write to the printer's socket.</param>
    /// <param name="DelayAfterMs">How long to wait after this segment is flushed before sending the next one.</param>
    private readonly record struct PrintSegment(byte[] Data, int DelayAfterMs);

    /// <summary>
    /// Thresholds the given bitmap to black/white by luminance, packs it into ESC/POS raster bytes, and prefixes the GS v 0 header for the given density mode.
    /// </summary>
    /// <param name="bitmap">The bitmap to pack - a dithered image or plain rendered text/lines, either works since this thresholds by luminance either way.</param>
    /// <param name="densityMode">The density mode byte (m) for the GS v 0 header - 0 prints the transmitted pixels 1:1, higher values double the transmitted data on one or both axes at the printer.</param>
    private static byte[] BuildRasterCommand(SKBitmap bitmap, byte densityMode)
    {
        int widthBytes = (bitmap.Width + 7) / 8;
        byte[] rasterBitmap = new byte[widthBytes * bitmap.Height];

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                int luminance = (pixel.Red + pixel.Green + pixel.Blue) / 3;
                if (luminance < 128)
                {
                    int rotatedY = bitmap.Height - 1 - y;
                    int rotatedX = bitmap.Width - 1 - x;
                    rasterBitmap[(rotatedY * widthBytes) + (rotatedX / 8)] |= (byte)(0x80 >> (rotatedX % 8));
                }
            }
        }

        int xL = widthBytes & 0xFF;
        int xH = (widthBytes >> 8) & 0xFF;
        int yL = bitmap.Height & 0xFF;
        int yH = (bitmap.Height >> 8) & 0xFF;
        byte[] header = [0x1D, 0x76, 0x30, densityMode, (byte)xL, (byte)xH, (byte)yL, (byte)yH];
        byte[] command = new byte[header.Length + rasterBitmap.Length];
        Array.Copy(header, command, header.Length);
        Array.Copy(rasterBitmap, 0, command, header.Length, rasterBitmap.Length);
        return command;
    }

    /// <summary>
    /// Converts the given bitmap to a 1-bit monochrome bitmap and splits it into ESC/POS byte segments, banded to at most MaxBandHeightPx rows each, all at normal density (m=0). Used for body text, which prints densely enough that a paced seam between bands isn't visible - PrintImageAsync sends a photo as its own, separately-built raster command instead (see BuildRasterCommand), since a seam is visible on continuous-tone content.
    /// </summary>
    /// <param name="bitmap">The bitmap to print, already sized to the printer's usable width.</param>
    private List<PrintSegment> BuildImageSegments(SKBitmap bitmap)
    {
        // Calculate the width in bytes (1 byte = 8 pixels).
        int widthBytes = (bitmap.Width + 7) / 8;
        byte[] rasterBitmap = new byte[widthBytes * bitmap.Height];

        // Iterate over each pixel in the bitmap, and threshold every pixel to black/white up front.
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                // Threshold to black/white using perceived luminance; treat dark pixels as printable dots.
                SKColor pixel = bitmap.GetPixel(x, y);
                int luminance = (pixel.Red + pixel.Green + pixel.Blue) / 3;
                if (luminance < 128)
                {
                    int rotatedY = bitmap.Height - 1 - y;
                    int rotatedX = bitmap.Width - 1 - x;
                    rasterBitmap[(rotatedY * widthBytes) + (rotatedX / 8)] |= (byte)(0x80 >> (rotatedX % 8));
                }
            }
        }

        // BandDelayMs scaled per row so any band shorter than a full one waits proportionally less.
        double delayMsPerRow = BandDelayMs / (double)MaxBandHeightPx;

        // Setup segment: initialize printer, then feed the configured number of lines before the image starts. This is just a fixed settle pause, not scaled to bandHeightPx - there's no band to wait on yet.
        List<PrintSegment> segments = [];
        segments.Add(new PrintSegment([0x1B, 0x40, 0x1B, 0x64, (byte)FeedBefore], BandDelayMs));

        // Split the raster data into bands of at most MaxBandHeightPx rows, each paced by how long it actually took to print - a shorter final band waits proportionally less than a full one.
        for (int bandStartRow = 0; bandStartRow < bitmap.Height; bandStartRow += MaxBandHeightPx)
        {
            int bandHeight = Math.Min(MaxBandHeightPx, bitmap.Height - bandStartRow);
            int bandByteOffset = bandStartRow * widthBytes;
            int bandByteLength = bandHeight * widthBytes;

            int xL = widthBytes & 0xFF;
            int xH = (widthBytes >> 8) & 0xFF;
            int yL = bandHeight & 0xFF;
            int yH = (bandHeight >> 8) & 0xFF;
            byte[] bandSegment = new byte[8 + bandByteLength];
            byte[] imageHeader = [0x1D, 0x76, 0x30, 0x00, (byte)xL, (byte)xH, (byte)yL, (byte)yH];
            Array.Copy(imageHeader, bandSegment, imageHeader.Length);
            Array.Copy(rasterBitmap, bandByteOffset, bandSegment, imageHeader.Length, bandByteLength);

            int delayAfterMs = (int)Math.Round(delayMsPerRow * bandHeight);
            segments.Add(new PrintSegment(bandSegment, delayAfterMs));
        }

        // Final segment: feed the configured number of lines after the image, then cut.
        segments.Add(new PrintSegment([0x1B, 0x64, (byte)FeedAfter, 0x1D, 0x56, 0x00], 0));

        return segments;
    }

    /// <summary>
    /// Opens a TCP connection to the configured printer and writes each segment in turn.
    /// </summary>
    /// <param name="segments">The ordered ESC/POS byte segments to send.</param>
    /// <returns>True if all segments were sent successfully, false otherwise.</returns>
    private async Task<bool> SendPacedAsync(List<PrintSegment> segments)
    {
        try
        {
            // Intialise the connecton.
            using TcpClient client = new();
            await client.ConnectAsync(PrinterIp, PrinterPort);
            using NetworkStream stream = client.GetStream();

            // Iterate through each segment.
            for (int i = 0; i < segments.Count; i++)
            {
                await stream.WriteAsync(segments[i].Data);
                await stream.FlushAsync();
                if (i < segments.Count - 1 && segments[i].DelayAfterMs > 0)
                    await Task.Delay(segments[i].DelayAfterMs);
            }
            return true;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
                logger.LogError(ex, "Failed to send print job to printer at {PrinterIp}:{PrinterPort}", PrinterIp, PrinterPort);
            return false;
        }
    }
}