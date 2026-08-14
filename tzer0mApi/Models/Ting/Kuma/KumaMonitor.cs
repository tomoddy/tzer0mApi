namespace tzer0mApi.Models.Ting.Kuma;

/// <summary>
/// The monitor configuration associated with a Kuma webhook notification.
/// </summary>
public sealed class KumaMonitor
{
    /// <summary>
    /// The monitor's display name in Kuma.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The URL being monitored, if applicable.
    /// </summary>
    public string? Url { get; init; }
}