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

        // Extract the full text annotation which gives the cleanest single string
        JsonElement root = doc.RootElement;
        string? text = root.GetProperty("responses")[0].GetProperty("fullTextAnnotation").GetProperty("text").GetString();

        // Log the raw text for debugging
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Vision API raw result: '{Text}'", text);

        // Return text
        return text;
    }
}