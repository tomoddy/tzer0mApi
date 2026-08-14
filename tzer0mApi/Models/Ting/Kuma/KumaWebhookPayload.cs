namespace tzer0mApi.Models.Ting.Kuma;

/// <summary>
/// The webhook payload sent by Uptime Kuma when a monitor's status changes.
/// </summary>
public sealed class KumaWebhookPayload
{
    /// <summary>
    /// The heartbeat that triggered this notification.
    /// </summary>
    public KumaHeartbeat? Heartbeat { get; init; }

    /// <summary>
    /// The monitor the heartbeat belongs to.
    /// </summary>
    public KumaMonitor? Monitor { get; init; }

    /// <summary>
    /// The pre-formatted message Kuma generated for this notification.
    /// </summary>
    public required string Msg { get; init; }
}