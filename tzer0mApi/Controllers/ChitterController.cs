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
}