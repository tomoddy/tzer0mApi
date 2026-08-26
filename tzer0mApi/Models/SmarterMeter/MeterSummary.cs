namespace tzer0mApi.Models.SmarterMeter;

/// <summary>
/// Summarises electricity usage and cost for key time periods.
/// </summary>
public sealed class MeterSummary
{
    /// <summary>
    /// The latest cumulative meter reading in kWh.
    /// </summary>
    public decimal CurrentReading { get; init; }

    /// <summary>
    /// Today's usage in kWh.
    /// </summary>
    public decimal TodayUsage { get; init; }

    /// <summary>
    /// Today's cost in £.
    /// </summary>
    public decimal TodayCost { get; init; }

    /// <summary>
    /// Last 7 days usage in kWh.
    /// </summary>
    public decimal WeekUsage { get; init; }

    /// <summary>
    /// Last 7 days cost in £.
    /// </summary>
    public decimal WeekCost { get; init; }

    /// <summary>
    /// Last 30 days usage in kWh.
    /// </summary>
    public decimal MonthUsage { get; init; }

    /// <summary>
    /// Last 30 days cost in £.
    /// </summary>
    public decimal MonthCost { get; init; }

    /// <summary>
    /// The UTC timestamp of the most recent meter reading capture.
    /// </summary>
    public DateTime LastCapturedAt { get; init; }

    /// <summary>
    /// Reading capture success rate as a percentage.
    /// </summary>
    public decimal SuccessRate { get; init; }

    /// <summary>
    /// Number of hours between scheduled captures, used by consumers to size lookback windows and thresholds.
    /// </summary>
    public int CaptureIntervalHours { get; init; }
}