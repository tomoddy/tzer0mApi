namespace tzer0mApi.Models.SmarterMeter;

/// <summary>
/// Represents a single electricity meter reading captured from a photo.
/// </summary>
public class MeterReading
{
    /// <summary>
    /// Auto-incremented primary key from the database.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The parsed meter value in kWh.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// The raw text returned by Tesseract before parsing.
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// Tesseract's mean confidence score for this reading (0.0 - 1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Path to the original image file on disk.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// When the photo was taken on the Pi.
    /// </summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>
    /// When the reading was written to the database.
    /// </summary>
    public DateTime RecordedAt { get; set; }
}