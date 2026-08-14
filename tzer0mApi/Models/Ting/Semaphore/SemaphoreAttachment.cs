namespace tzer0mApi.Models.Ting.Semaphore;

/// <summary>
/// Represents a single Slack-style attachment within a Semaphore webhook payload, containing the task result details.
/// </summary>
public class SemaphoreAttachment
{
    /// <summary>
    /// The task template name, e.g. "Task: Update Tyrion".
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// A link back to the Semaphore execution page for this task run.
    /// </summary>
    public string? TitleLink { get; set; }

    /// <summary>
    /// The execution result text, e.g. "execution ID #268, status: SUCCESS!".
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Slack-style status color: "good" indicates success, "danger" indicates failure.
    /// </summary>
    public string? Color { get; set; }
}