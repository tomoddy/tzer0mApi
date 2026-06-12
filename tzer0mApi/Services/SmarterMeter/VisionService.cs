using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace tzer0mApi.Services.SmarterMeter;

/// <summary>
/// Sends preprocessed meter images to the Google Cloud Vision API and extracts digit text.
/// </summary>
public partial class VisionService(ILogger<VisionService> logger, IConfiguration configuration, HttpClient httpClient)
{
    /// <summary>
    /// Api key for Google Cloud Vision API, loaded from configuration.
    /// </summary>
    private readonly string ApiKey = configuration["SmarterMeter:GoogleVision:ApiKey"] ?? throw new InvalidOperationException("SmarterMeter:GoogleVision:ApiKey is not configured");

    /// <summary>
    /// Matches a 5-digit meter reading split by a space e.g. "562 18".
    /// </summary>
    [GeneratedRegex(@"\b(\d{2,3})\s(\d{2,3})\b")]
    private static partial Regex SplitMeterReadingRegex();

    /// <summary>
    /// Matches a contiguous 5-digit number.
    /// </summary>
    [GeneratedRegex(@"\b\d{5}\b")]
    private static partial Regex FiveDigitRegex();

    /// <summary>
    /// Sends image bytes to Google Cloud Vision and returns the detected text.
    /// </summary>
    /// <param name="imageBytes">The preprocessed image as a PNG byte array.</param>
    /// <returns>The raw text detected in the image, or null if the request failed.</returns>
    public async Task<string?> DetectTextAsync(byte[] imageBytes)
    {
        // Get image and create request
        string base64Image = Convert.ToBase64String(imageBytes);
        object requestBody = new
        {
            requests = new[]
            {
                new
                {
                    image = new { content = base64Image },
                    features = new[] { new { type = "TEXT_DETECTION" } }
                }
            }
        };

        // Add string content
        string json = JsonSerializer.Serialize(requestBody);
        using StringContent content = new(json, Encoding.UTF8, "application/json");

        // Send request
        string url = $"https://vision.googleapis.com/v1/images:annotate?key={ApiKey}";
        HttpResponseMessage response = await httpClient.PostAsync(url, content);

        // Check response
        if (!response.IsSuccessStatusCode)
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("Vision API returned {StatusCode}", response.StatusCode);
            return null;
        }

        // Deserialize response
        string responseJson = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(responseJson);

        JsonElement root = doc.RootElement;

        // Check if fullTextAnnotation exists
        if (!root.GetProperty("responses")[0].TryGetProperty("fullTextAnnotation", out JsonElement fullText))
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("Vision API returned no text annotations");
            return null;
        }

        string? rawText = fullText.GetProperty("text").GetString();

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Vision API raw result: '{Text}'", rawText);

        // Find a 5-digit meter reading, potentially split by a space
        Match match = SplitMeterReadingRegex().Match(rawText ?? string.Empty);

        if (match.Success)
            return match.Groups[1].Value + match.Groups[2].Value;

        // Fallback: find any 5-digit number
        match = FiveDigitRegex().Match(rawText ?? string.Empty);

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Vision API meter value: '{Text}'", match.Value);

        return match.Success ? match.Value : null;
    }
}