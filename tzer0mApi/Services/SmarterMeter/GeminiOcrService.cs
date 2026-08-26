using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace tzer0mApi.Services.SmarterMeter;

/// <summary>
/// Reads meter digits from an image using the Gemini API (gemini-flash-lite-latest by default).
/// </summary>
public class GeminiOcrService(HttpClient httpClient, ILogger<GeminiOcrService> logger, IConfiguration configuration)
{
    /// <summary>
    /// Prompt sent to the model.
    /// </summary>
    private const string Prompt = "You are reading a utility meter display in this photo. Return ONLY the numeric reading shown on the meter, digit by digit, with no units, no commentary, and no extra text. Ignore any decimal point, comma, or other punctuation on the display or dials (including red 'hundredths' sub-dials) - concatenate the digits into a single whole number with no decimal point, no comma, and no spaces. If any digit is genuinely unreadable, use '?' in that position. If there is no meter display visible, respond with 'NO_READING'.";

    /// <summary>
    /// Sends the given image bytes to Gemini and returns the raw text of the model's reply, or null if the request failed or returned no text.
    /// </summary>
    /// <param name="imageBytes">Raw JPEG/PNG bytes of the meter photo.</param>
    /// <returns>The raw text returned by the model, or null on failure.</returns>
    public async Task<string?> DetectTextAsync(byte[] imageBytes)
    {
        // Retrieve the Gemini API key and model from configuration, defaulting to "gemini-flash-lite-latest" if not specified.
        string apiKey = configuration["SmarterMeter:Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey is not configured");
        string model = configuration["SmarterMeter:Gemini:Model"] ?? "gemini-flash-lite-latest";

        // Convert the image bytes to a base64 string for inclusion in the request body and construct the request.
        string base64Image = Convert.ToBase64String(imageBytes);
        string requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
        GeminiRequest requestBody = new([new([new(InlineData: new GeminiInlineData("image/jpeg", base64Image)), new(Text: Prompt)])]);

        // Send the request to the Gemini API, with the key in a header rather than the query string so it doesn't end up in URL-based logging.
        using HttpRequestMessage request = new(HttpMethod.Post, requestUrl) { Content = JsonContent.Create(requestBody) };
        request.Headers.Add("x-goog-api-key", apiKey);
        HttpResponseMessage response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync();
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("Gemini API returned {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            return null;
        }

        // Parse the response JSON into our GeminiResponse record and extract the text from the first candidate's first part.
        GeminiResponse? parsed = await response.Content.ReadFromJsonAsync<GeminiResponse>();
        string? text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim();

        // If the text is null, empty, whitespace, or "NO_READING", return null.
        if (string.IsNullOrWhiteSpace(text) || text == "NO_READING")
            return null;

        // Return the extracted text.
        return text;
    }

    /// <summary>
    /// Top-level request body sent to the Gemini generateContent endpoint.
    /// </summary>
    private record GeminiRequest([property: JsonPropertyName("contents")] GeminiContent[] Contents);

    /// <summary>
    /// A single turn of content, made up of one or more parts (text and/or inline image data).
    /// </summary>
    private record GeminiContent([property: JsonPropertyName("parts")] GeminiPart[] Parts);

    /// <summary>
    /// One part of a content turn - either a text prompt or an inline image, depending on which property is set.
    /// </summary>
    private record GeminiPart([property: JsonPropertyName("text")] string? Text = null, [property: JsonPropertyName("inline_data")] GeminiInlineData? InlineData = null);

    /// <summary>
    /// Base64-encoded image data and its MIME type, embedded directly in the request.
    /// </summary>
    private record GeminiInlineData([property: JsonPropertyName("mime_type")] string MimeType, [property: JsonPropertyName("data")] string Data);

    /// <summary>
    /// Top-level response body returned by the Gemini generateContent endpoint.
    /// </summary>
    private record GeminiResponse([property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates);

    /// <summary>
    /// A single candidate reply from the model, containing its generated content.
    /// </summary>
    private record GeminiCandidate([property: JsonPropertyName("content")] GeminiContent? Content);
}