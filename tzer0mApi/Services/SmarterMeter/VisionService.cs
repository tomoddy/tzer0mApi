using System.Text;
using System.Text.Json;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace tzer0mApi.Services.SmarterMeter;

/// <summary>
/// Sends preprocessed meter images to the Google Cloud Vision API and extracts digit text.
/// </summary>
public class VisionService(ILogger<VisionService> logger, IConfiguration configuration, HttpClient httpClient)
{
    /// <summary>
    /// Api key for Google Cloud Vision API, loaded from configuration.
    /// </summary>
    private readonly string ApiKey = configuration["SmarterMeter:GoogleVision:ApiKey"] ?? throw new InvalidOperationException("SmarterMeter:GoogleVision:ApiKey is not configured");

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

        // Index 0 is the full text, individual blocks start at index 1
        JsonElement root = doc.RootElement;
        JsonElement annotations = root.GetProperty("responses")[0].GetProperty("textAnnotations");

        // Find kWh block and take the two blocks before it
        string? text = null;
        int length = annotations.GetArrayLength();
        for (int i = 2; i < length; i++)
        {
            string? blockText = annotations[i].GetProperty("description").GetString();
            if (blockText?.Equals("kWh", StringComparison.OrdinalIgnoreCase) == true)
            {
                string? part1 = annotations[i - 2].GetProperty("description").GetString();
                string? part2 = annotations[i - 1].GetProperty("description").GetString();
                text = $"{part1}{part2}";
                break;
            }
        }

        // Return text
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Vision API meter value: '{Text}'", text);
        return text;
    }
}