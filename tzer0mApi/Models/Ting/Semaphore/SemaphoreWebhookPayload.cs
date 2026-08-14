namespace tzer0mApi.Models.Ting.Semaphore;

/// <summary>
/// Represents the Slack-formatted webhook payload sent by Semaphore UI on task completion.
/// </summary>
public class SemaphoreWebhookPayload
{
    /// <summary>
    /// The list of Slack-style attachments Semaphore includes in the notification. Semaphore always sends exactly one.
    /// </summary>
    public List<SemaphoreAttachment>? Attachments { get; set; }
}