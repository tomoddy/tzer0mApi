using Microsoft.AspNetCore.Mvc;
using tzer0mApi.Services.Chitter;

namespace tzer0mApi.Controllers;

/// <summary>
/// Handles print jobs for the Aures ODP 333 receipt printer.
/// </summary>
/// <param name="printService">The service used to render and send print jobs.</param>
[ApiController]
[Route("Chitter")]
public class ChitterController(ChitterPrintService printService) : ControllerBase
{
    /// <summary>
    /// The maximum size, in bytes, of an uploaded image.
    /// </summary>
    private const long MaxImageBytes = 15 * 1024 * 1024;

    /// <summary>
    /// Renders the given text (with a divider-and-timestamp footer) and prints it.
    /// </summary>
    /// <param name="text">The plain text to print, sent as the raw request body.</param>
    /// <returns>200 on success, or 502 if the printer could not be reached.</returns>
    [HttpPost("Text", Name = "Print Text")]
    public async Task<IActionResult> PrintText([FromBody] string text)
    {
        // Validate that the request body contains non-empty text.
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest(new { error = "Request body must contain non-empty text." });

        // Validate that the request body does not exceed 1024 characters.
        if (text.Length > 1024)
            return StatusCode(413, new { error = "Request body must not exceed 1024 characters." });

        // Send the text to the print service and check if it was sent successfully.
        bool sent = await printService.PrintTextAsync(text);
        if (!sent)
            return StatusCode(502, new { error = "Failed to reach printer." });

        // Return a success response indicating that the text was sent to the printer.
        return Ok(new { message = "Sent to printer" });
    }

    /// <summary>
    /// Resizes, dithers, and prints the given image.
    /// </summary>
    /// <param name="image">The image file, sent as multipart/form-data under the field name "image".</param>
    /// <returns>200 on success, 400 if the upload is missing/invalid, 413 if it's too large, or 502 if the printer could not be reached.</returns>
    [HttpPost("Image", Name = "Print Image")]
    public async Task<IActionResult> PrintImage(IFormFile? image)
    {
        // Validate that the request contains a non-empty image file.
        if (image is null || image.Length == 0)
            return BadRequest(new { error = "Request must contain a non-empty image file." });

        // Validate that the file does not exceed the maximum upload size.
        if (image.Length > MaxImageBytes)
            return StatusCode(413, new { error = $"Image must not exceed {MaxImageBytes / (1024 * 1024)} MB." });

        // Validate that the file is actually an image, based on its declared content type.
        if (string.IsNullOrEmpty(image.ContentType) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Uploaded file must be an image." });

        // Send the image to the print service and check if it was sent successfully.
        bool sent;
        await using (Stream stream = image.OpenReadStream())
        {
            try
            {
                sent = await printService.PrintImageAsync(stream);
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { error = "Could not read the uploaded image - is it a valid image file?" });
            }
        }

        // If the print service failed to send the image to the printer, return a 502 Bad Gateway response.
        if (!sent)
            return StatusCode(502, new { error = "Failed to reach printer." });

        // Return a success response indicating that the image was sent to the printer.
        return Ok(new { message = "Sent to printer" });
    }
}