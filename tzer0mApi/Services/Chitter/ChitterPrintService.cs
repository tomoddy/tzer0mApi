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
    /// Renders the given body text with a divider-and-timestamp footer beneath it, and sends the resulting image to the printer.
    /// </summary>
    /// <param name="bodyText">The text to print above the footer.</param>
    /// <returns>True if the payload was sent successfully, false otherwise.</returns>
    public async Task<bool> PrintTextAsync(string bodyText)
    {
        // Setup fonts and colours.
        using SKTypeface typeface = LoadTypeface();
        using SKFont bodyFont = new(typeface, BodyFontSize);
        using SKFont footerFont = new(typeface, FooterFontSize);
        using SKPaint paint = new() { Color = SKColors.Black, IsAntialias = true };

        // Calculate the usable width for text and set the footer information.
        int contentWidth = WidthPx - (MarginPx * 2);
        string footerDivider = new('-', 40);
        string footerTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Wrap the body text and set the footer lines.
        List<string> bodyLines = WrapText(bodyText, bodyFont, paint, contentWidth);
        List<string> footerLines = [footerDivider, footerTimestamp];

        // Set the line height for each font and calculate the total image height.
        float bodyLineHeight = BodyFontSize * 1.3f;
        float footerLineHeight = FooterFontSize * 1.3f;
        int bodyBlockHeight = (int)Math.Ceiling(bodyLines.Count * bodyLineHeight);
        int footerBlockHeight = (int)Math.Ceiling(footerLines.Count * footerLineHeight);
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
                canvas.DrawText(line, MarginPx, y, SKTextAlign.Left, bodyFont, paint);
                y += bodyLineHeight;
            }

            // Calculate the Y position for the footer and draw the footer.
            y = MarginPx + bodyBlockHeight + MarginPx + FooterFontSize;
            foreach (string line in footerLines)
            {
                canvas.DrawText(line, MarginPx, y, SKTextAlign.Left, footerFont, paint);
                y += footerLineHeight;
            }
        }

        // Convert the bitmap to a 1-bit monochrome raster image and send it to the printer.
        byte[] payload = BuildImagePayload(bitmap);
        return await SendAsync(payload);
    }

    /// <summary>
    /// Loads the bundled Space Grotesk typeface from disk.
    /// </summary>
    private SKTypeface LoadTypeface()
    {
        string fontRelativePath = config["Chitter:Image:FontPath"] ?? "Assets/Fonts/SpaceGrotesk.ttf";
        string fontPath = Path.Combine(env.ContentRootPath, fontRelativePath);
        return SKTypeface.FromFile(fontPath) ?? throw new InvalidOperationException($"Could not load font at {fontPath}");
    }

    /// <summary>
    /// Splits the given text into lines that each fit within the given pixel width, wrapping on word boundaries.
    /// </summary>
    /// <param name="text">The text to wrap. Existing newlines are treated as forced line breaks.</param>
    /// <param name="font">The font used to measure text width.</param>
    /// <param name="paint">The paint used to measure text width.</param>
    /// <param name="maxWidth">The maximum line width, in pixels.</param>
    private static List<string> WrapText(string text, SKFont font, SKPaint paint, int maxWidth)
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
                if (font.MeasureText(candidate, paint) <= maxWidth)
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
    /// Converts the given bitmap to a 1-bit monochrome bitmap and wraps it in an ESC/POS byte sequence: initialize printer, feed, raster image, feed, and cut.
    /// </summary>
    /// <param name="bitmap">The bitmap to print, already sized to the printer's usable width.</param>
    private byte[] BuildImagePayload(SKBitmap bitmap)
    {
        // Calculate the width in bytes (1 byte = 8 pixels) and create a byte array to hold the raster data.
        int widthBytes = (bitmap.Width + 7) / 8;
        byte[] rasterBitmap = new byte[widthBytes * bitmap.Height];

        // Iterate over each pixel in the bitmap, converting it to black or white based on luminance, and set the corresponding bit in the raster byte array.
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                // Threshold to black/white using perceived luminance; treat dark pixels as printable dots.
                SKColor pixel = bitmap.GetPixel(x, y);
                int luminance = (pixel.Red + pixel.Green + pixel.Blue) / 3;
                if (luminance < 128)
                    rasterBitmap[(y * widthBytes) + (x / 8)] |= (byte)(0x80 >> (x % 8));
            }
        }

        // Create a memory stream and initialize it.
        using MemoryStream stream = new();
        byte[] init = [0x1B, 0x40];
        stream.Write(init, 0, init.Length);

        // Feed the specified number of lines before printing the image.
        byte[] feedBefore = [0x1B, 0x64, (byte)FeedBefore];
        stream.Write(feedBefore, 0, feedBefore.Length);

        // Send the raster image using the ESC/POS GS v 0 command, which requires the width and height in bytes and pixels, respectively.
        int xL = widthBytes & 0xFF;
        int xH = (widthBytes >> 8) & 0xFF;
        int yL = bitmap.Height & 0xFF;
        int yH = (bitmap.Height >> 8) & 0xFF;
        byte[] imageHeader = [0x1D, 0x76, 0x30, 0x00, (byte)xL, (byte)xH, (byte)yL, (byte)yH];
        stream.Write(imageHeader, 0, imageHeader.Length);
        stream.Write(rasterBitmap, 0, rasterBitmap.Length);

        // Feed the specified number of lines after printing the image.
        byte[] feedAfter = [0x1B, 0x64, (byte)FeedAfter];
        stream.Write(feedAfter, 0, feedAfter.Length);

        // Send the cut command to the printer.
        byte[] cut = [0x1D, 0x56, 0x00];
        stream.Write(cut, 0, cut.Length);

        // Return the complete ESC/POS payload as a byte array.
        return stream.ToArray();
    }

    /// <summary>
    /// Opens a TCP connection to the configured printer and writes the given payload.
    /// </summary>
    /// <param name="payload">The raw ESC/POS bytes to send.</param>
    /// <returns>True if the payload was sent successfully, false otherwise.</returns>
    private async Task<bool> SendAsync(byte[] payload)
    {
        try
        {
            using TcpClient client = new();
            await client.ConnectAsync(PrinterIp, PrinterPort);
            using NetworkStream stream = client.GetStream();
            await stream.WriteAsync(payload);
            await stream.FlushAsync();
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