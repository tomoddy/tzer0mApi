namespace tzer0mApi.Models.Ting;

/// <summary>
/// A single heartbeat check result from Uptime Kuma.
/// </summary>
public sealed class KumaHeartbeat
{
    /// <summary>
    /// The heartbeat status: 0 = down, 1 = up, 2 = pending, 3 = maintenance.
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// The check result message, e.g. "200 - OK".
    /// </summary>
    public string? Msg { get; init; }

    /// <summary>
    /// Whether this heartbeat represents a status change worth notifying on.
    /// </summary>
    public bool Important { get; init; }
}