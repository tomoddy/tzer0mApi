namespace tzer0mApi.Services.SmarterMeter.ImageProcessing;

/// <summary>
/// Represents the result of processing a meter image through the OCR pipeline.
/// </summary>
public class ImageProcessingResult
{
    /// <summary>
    /// Whether the image was successfully processed and a value parsed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The raw text returned by Tesseract before parsing.
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// The parsed meter value.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Tesseract's mean confidence score (0.0 - 1.0).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Path to the debug image saved after preprocessing.
    /// </summary>
    public string? DebugImagePath { get; set; }
}