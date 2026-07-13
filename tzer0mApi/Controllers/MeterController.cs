using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using tzer0mApi.Models.SmarterMeter;
using tzer0mApi.Services.SmarterMeter;

namespace tzer0mApi.Controllers;

/// <summary>
/// Handles incoming meter image submissions and reading retrieval.
/// </summary>
[ApiController]
[Route("SmarterMeter")]
public class MeterController(VisionService visionService, DatabaseService databaseService, ILogger<MeterController> logger, IConfiguration config, CalculationService calculationService) : ControllerBase
{
    /// <summary>
    /// Image capture path.
    /// </summary>
    private readonly string CapturePath = config["SmarterMeter:Storage:CapturePath"] ?? throw new InvalidOperationException("SmarterMeter:Storage:CapturePath is not configured");

    /// <summary>
    /// The configured tariff periods.
    /// </summary>
    private readonly List<Tariff> Tariffs = config.GetSection("SmarterMeter:Tariffs").Get<List<Tariff>>() ?? throw new InvalidOperationException("SmarterMeter:Tariffs is not configured");


    /// <summary>
    /// Accepts a filename, reads the image from the NAS, runs OCR, and stores the result.
    /// </summary>
    /// <param name="filename">The filename of the image on the NAS capture path.</param>
    /// <param name="capturedAt">Optional ISO 8601 timestamp of when the photo was taken. Defaults to UTC now.</param>
    /// <returns>The saved reading on success, or an error response if the file is missing or OCR fails.</returns>
    [HttpPost("Reading", Name = "Submit Reading")]
    public async Task<IActionResult> SubmitReading([FromQuery] string filename, [FromQuery] DateTime? capturedAt)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return BadRequest(new { error = "No filename provided" });

        // Prevent path traversal attacks
        string safeFilename = Path.GetFileName(filename);
        string imagePath = Path.Combine(CapturePath, safeFilename);

        if (!System.IO.File.Exists(imagePath))
            return NotFound(new { error = $"File not found: {safeFilename}" });

        // Read raw image bytes from NAS
        byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);

        // Send directly to Google Cloud Vision
        string? rawText = await visionService.DetectTextAsync(imageBytes);
        if (string.IsNullOrWhiteSpace(rawText))
            return UnprocessableEntity(new { error = "Vision API returned no text" });

        // Strip anything that isn't a digit
        string digitsOnly = new([.. rawText.Where(char.IsDigit)]);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Digits extracted: '{Digits}'", digitsOnly);

        // Parse the first 5-digit number from the detected text
        if (!decimal.TryParse(digitsOnly, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
            return UnprocessableEntity(new { error = $"Could not parse '{rawText}' as a number" });

        // Validate reading is greater than the last recorded value
        MeterReading? lastReading = (await databaseService.GetRecentReadingsAsync(1)).FirstOrDefault();
        if (lastReading != null && (value < lastReading.Value || value > lastReading.Value + 20))
        {
            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning("Reading {Value} is less than last reading {LastValue} or exceeds the allowed range", value, lastReading.Value);
            return UnprocessableEntity(new { error = $"Reading {value} is less than last reading {lastReading.Value} or exceeds the allowed range" });
        }

        // Persist to database
        MeterReading reading = await databaseService.InsertReadingAsync(new MeterReading
        {
            Value = value,
            RawText = rawText,
            Confidence = 1.0f,
            ImagePath = imagePath,
            CapturedAt = capturedAt?.ToUniversalTime() ?? DateTime.UtcNow
        });

        // Return the saved reading
        return Ok(new
        {
            id = reading.Id,
            value = reading.Value,
            raw_text = reading.RawText,
            confidence = reading.Confidence,
            captured_at = reading.CapturedAt,
            recorded_at = reading.RecordedAt
        });
    }

    /// <summary>
    /// Returns the most recent meter readings.
    /// </summary>
    /// <param name="count">Number of readings to return. Defaults to 48 (two days at hourly intervals).</param>
    [HttpGet("Readings", Name = "Get Readings")]
    public async Task<IActionResult> GetReadings([FromQuery] int count = 48)
    {
        IEnumerable<MeterReading> readings = await databaseService.GetRecentReadingsAsync(count);
        return Ok(readings);
    }

    /// <summary>
    /// Returns all configured tariff periods.
    /// </summary>
    [HttpGet("Tariffs", Name = "Get Tariffs")]
    public IActionResult GetTariffs()
    {
        return Ok(Tariffs);
    }

    /// <summary>
    /// Returns a summary of electricity usage and cost for today, last 7 days, and last 30 days.
    /// </summary>
    [HttpGet("Summary", Name = "Get Summary")]
    public async Task<IActionResult> GetSummary()
    {
        List<MeterReading> readings = [.. await databaseService.GetRecentReadingsAsync(500)];

        DateTime cutoff = DateTime.UtcNow.AddHours(-200);
        decimal successRate = Math.Round(
            Math.Min(readings.Count(r => r.CapturedAt >= cutoff) / 100m * 100m, 100m), 1);

        MeterSummary summary = calculationService.Calculate(readings, successRate);

        return Ok(summary);
    }
}