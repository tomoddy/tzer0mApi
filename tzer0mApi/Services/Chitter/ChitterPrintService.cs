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
    /// Path, relative to the content root, of the monochrome emoji font used as a fallback for characters
    /// Space Grotesk doesn't cover.
    /// </summary>
    private readonly string EmojiFontRelativePath = config["Chitter:Image:EmojiFontPath"] ?? "Assets/Fonts/NotoEmoji-Medium.ttf";

    /// <summary>
    /// Maximum height, in pixels, of a single raster image command.
    /// </summary>
    private readonly int MaxBandHeightPx = config.GetValue<int?>("Chitter:Image:MaxBandHeightPx") ?? 48;

    /// <summary>
    /// Delay, in milliseconds, after each image band is flushed to the printer, giving it time to physically
    /// print and drain its receive buffer before the next band arrives.
    /// </summary>
    private readonly int BandDelayMs = config.GetValue<int?>("Chitter:Image:BandDelayMs") ?? 700;

    /// <summary>
    /// Renders the given body text with a divider-and-timestamp footer beneath it, and sends the resulting image to the printer.
    /// </summary>
    /// <param name="bodyText">The text to print above the footer.</param>
    /// <returns>True if the payload was sent successfully, false otherwise.</returns>
    public async Task<bool> PrintTextAsync(string bodyText)
    {
        // Setup fonts and colours.
        using SKTypeface typeface = LoadTypeface(config["Chitter:Image:FontPath"] ?? "Assets/Fonts/SpaceGrotesk-Medium.ttf");
        using SKTypeface emojiTypeface = LoadTypeface(EmojiFontRelativePath);
        using SKFont bodyFont = new(typeface, BodyFontSize);
        using SKFont bodyEmojiFont = new(emojiTypeface, BodyFontSize);
        using SKFont footerFont = new(typeface, FooterFontSize);
        using SKFont footerEmojiFont = new(emojiTypeface, FooterFontSize);
        using SKPaint paint = new() { Color = SKColors.Black, IsAntialias = true };

        // Calculate the usable width for text and set the footer information.
        int contentWidth = WidthPx - (MarginPx * 2);
        string footerTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Wrap the body text.
        List<string> bodyLines = WrapText(bodyText, bodyFont, bodyEmojiFont, paint, contentWidth);

        // Set the line height for each font.
        float bodyLineHeight = BodyFontSize * 1.3f;
        float footerLineHeight = FooterFontSize * 1.3f;
        const int DividerHeight = 24;
        int bodyBlockHeight = (int)Math.Ceiling(bodyLines.Count * bodyLineHeight);
        int footerBlockHeight = DividerHeight + (int)Math.Ceiling(footerLineHeight);
        int totalHeight = MarginPx + bodyBlockHeight + MarginPx + footerBlockHeight + MarginPx;

        // Create a bitmap and erase it to white.
        using SKBitmap bitmap = new(WidthPx, totalHeight);
        bitmap.Erase(SKColors.White);
        using (SKCanvas canvas = new(bitmap))
        {
            // Calculate the Y position for each line and draw the body.
            float y = MarginPx + BodyFontSize;
            foreach (string line in bodyLines)
            {
                DrawMixedText(canvas, line, MarginPx, y, bodyFont, bodyEmojiFont, paint);
                y += bodyLineHeight;
            }

            // Draw the footer divider as an actual line spanning the full content width, then the timestamp beneath it.
            float dividerY = MarginPx + bodyBlockHeight + MarginPx + (DividerHeight / 2f);
            canvas.DrawLine(MarginPx, dividerY, MarginPx + contentWidth, dividerY, paint);
             
            // Draw the timestamp.
            float timestampY = MarginPx + bodyBlockHeight + MarginPx + DividerHeight + FooterFontSize;
            DrawMixedText(canvas, footerTimestamp, MarginPx, timestampY, footerFont, footerEmojiFont, paint);
        }

        // Convert the bitmap to a 1-bit monochrome raster image, split into paced bands, and send it to the printer.
        List<byte[]> segments = BuildImageSegments(bitmap);
        return await SendPacedAsync(segments);
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
    /// Picks the primary font if its typeface contains a glyph for the given rune, otherwise falls back to the emoji font.
    /// </summary>
    private static SKFont FontForRune(System.Text.Rune rune, SKFont primaryFont, SKFont emojiFont)
    {
        if (primaryFont.ContainsGlyph(rune.Value))
            return primaryFont;
        if (emojiFont.ContainsGlyph(rune.Value))
            return emojiFont;
        return primaryFont;
    }

    /// <summary>
    /// Splits the given text into runs of consecutive runes that resolve to the same font.
    /// </summary>
    /// <param name="text">The text to split into runs.</param>
    /// <param name="primaryFont">The main body/footer font.</param>
    /// <param name="emojiFont">The fallback emoji font.</param>
    private static List<(string Text, SKFont Font)> SplitIntoFontRuns(string text, SKFont primaryFont, SKFont emojiFont)
    {
        // Create a list of runs, each containing the text and the font used for that run.
        List<(string Text, SKFont Font)> runs = [];
        System.Text.StringBuilder currentRun = new();
        SKFont? currentFont = null;

        // Iterate through each rune in the text.
        foreach (System.Text.Rune rune in text.EnumerateRunes())
        {
            // Determine which font to use for this rune.
            SKFont font = FontForRune(rune, primaryFont, emojiFont);
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
    /// <param name="primaryFont">The main body/footer font.</param>
    /// <param name="emojiFont">The fallback emoji font.</param>
    /// <param name="paint">The paint used to measure text width.</param>
    private static float MeasureMixedText(string text, SKFont primaryFont, SKFont emojiFont, SKPaint paint)
    {
        float total = 0f;
        foreach ((string runText, SKFont runFont) in SplitIntoFontRuns(text, primaryFont, emojiFont))
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
    /// <param name="primaryFont">The main body/footer font.</param>
    /// <param name="emojiFont">The fallback emoji font.</param>
    /// <param name="paint">The paint used to draw and measure text.</param>
    private static void DrawMixedText(SKCanvas canvas, string text, float x, float y, SKFont primaryFont, SKFont emojiFont, SKPaint paint)
    {
        float currentX = x;
        foreach ((string runText, SKFont runFont) in SplitIntoFontRuns(text, primaryFont, emojiFont))
        {
            canvas.DrawText(runText, currentX, y, SKTextAlign.Left, runFont, paint);
            currentX += runFont.MeasureText(runText, paint);
        }
    }

    /// <summary>
    /// Splits the given text into lines that each fit within the given pixel width, wrapping on word boundaries.
    /// </summary>
    /// <param name="text">The text to wrap. Existing newlines are treated as forced line breaks.</param>
    /// <param name="font">The primary font used to measure text width.</param>
    /// <param name="emojiFont">The fallback emoji font used to measure any characters the primary font doesn't cover.</param>
    /// <param name="paint">The paint used to measure text width.</param>
    /// <param name="maxWidth">The maximum line width, in pixels.</param>
    private static List<string> WrapText(string text, SKFont font, SKFont emojiFont, SKPaint paint, int maxWidth)
    {
        // Split the text into paragraphs and then wrap each paragraph into lines.
        List<string> lines = [];
        foreach (string paragraph in text.Split('\n'))
        {
            // Split the paragraph into words, skip if the line has no words.
            string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            // Build lines by adding words until the line exceeds the maximum width.
            string currentLine = words[0];
            for (int i = 1; i < words.Length; i++)
            {
                string candidate = currentLine + " " + words[i];
                if (MeasureMixedText(candidate, font, emojiFont, paint) <= maxWidth)
                {
                    currentLine = candidate;
                }
                else
                {
                    lines.Add(currentLine);
                    currentLine = words[i];
                }
            }

            // Add the last line of the paragraph.
            lines.Add(currentLine);
        }

        // Return the wrapped lines.
        return lines;
    }

    /// <summary>
    /// Converts the given bitmap to a 1-bit monochrome bitmap and splits it into ESC/POS byte segments.
    /// </summary>
    /// <param name="bitmap">The bitmap to print, already sized to the printer's usable width.</param>
    private List<byte[]> BuildImageSegments(SKBitmap bitmap)
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

        // Setup segment: initialize printer, then feed the configured number of lines before the image starts.
        List<byte[]> segments = [];
        segments.Add([0x1B, 0x40, 0x1B, 0x64, (byte)FeedBefore]);

        // Split the raster data into bands of at most MaxBandHeightPx rows.
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
            segments.Add(bandSegment);
        }

        // Final segment: feed the configured number of lines after the image, then cut.
        segments.Add([0x1B, 0x64, (byte)FeedAfter, 0x1D, 0x56, 0x00]);

        return segments;
    }

    /// <summary>
    /// Opens a TCP connection to the configured printer and writes each segment in turn.
    /// </summary>
    /// <param name="segments">The ordered ESC/POS byte segments to send.</param>
    /// <returns>True if all segments were sent successfully, false otherwise.</returns>
    private async Task<bool> SendPacedAsync(List<byte[]> segments)
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
                await stream.WriteAsync(segments[i]);
                await stream.FlushAsync();
                if (i < segments.Count - 1)
                    await Task.Delay(BandDelayMs);
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