using tzer0mApi.Models.SmarterMeter;

namespace tzer0mApi.Services.SmarterMeter;

/// <summary>
/// Calculates electricity usage and cost summaries from meter readings and tariffs.
/// </summary>
public class CalculationService(IConfiguration config)
{
    /// <summary>
    /// The configured tariff periods.
    /// </summary>
    private readonly List<Tariff> Tariffs = config.GetSection("SmarterMeter:Tariffs").Get<List<Tariff>>() ?? [];

    /// <summary>
    /// Calculates a full usage and cost summary for today, last 7 days, and last 30 days.
    /// </summary>
    /// <param name="readings">All available meter readings, in any order.</param>
    /// <param name="successRate">Pre-calculated reading success rate percentage.</param>
    public MeterSummary Calculate(List<MeterReading> readings, decimal successRate)
    {
        // Return empty summary if no readings are available
        if (readings.Count == 0)
            return new MeterSummary();

        // Sort readings by timestamp, most recent first
        readings = [.. readings.OrderBy(r => r.CapturedAt)];

        // Set the time periods for today, last 7 days, and last 30 days
        DateTime today = DateTime.UtcNow.Date;
        DateTime weekStart = today.AddDays(-6);
        DateTime monthStart = today.AddDays(-29);

        // Get the readings for each time period
        List<MeterReading> todayReadings = [.. readings.Where(r => r.CapturedAt.Date == today)];
        List<MeterReading> weekReadings = [.. readings.Where(r => r.CapturedAt.ToLocalTime().Date > weekStart)];
        List<MeterReading> monthReadings = [.. readings.Where(r => r.CapturedAt.ToLocalTime().Date > monthStart)];

        // Calculate usage and cost for each time period
        decimal todayUsage = todayReadings.Count > 1 ? todayReadings.Last().Value - todayReadings.First().Value : 0;
        decimal weekUsage = weekReadings.Count > 1 ? weekReadings.Last().Value - weekReadings.First().Value : 0;
        decimal monthUsage = monthReadings.Count > 1 ? monthReadings.Last().Value - monthReadings.First().Value : 0;

        // Return the summary with calculated values
        return new MeterSummary
        {
            CurrentReading = readings.Last().Value,
            TodayUsage = todayUsage,
            TodayCost = CalculateCostForRange(todayReadings, today, today),
            WeekUsage = weekUsage,
            WeekCost = CalculateCostForRange(weekReadings, weekStart, today),
            MonthUsage = monthUsage,
            MonthCost = CalculateCostForRange(monthReadings, monthStart, today),
            SuccessRate = successRate
        };
    }

    /// <summary>
    /// Calculates the cost in £ for a list of readings within a date range, clipped per tariff period.
    /// </summary>
    /// <param name="readings">Readings within the period.</param>
    /// <param name="rangeStart">The start date of the range.</param>
    /// <param name="rangeEnd">The end date of the range.</param>
    public decimal CalculateCostForRange(List<MeterReading> readings, DateTime rangeStart, DateTime rangeEnd)
    {
        decimal totalCost = 0;
        foreach (Tariff tariff in Tariffs)
        {
            // Clip the tariff period to the requested range
            DateTime periodStart = rangeStart.Date > tariff.StartDate.Date ? rangeStart.Date : tariff.StartDate.Date;
            DateTime periodEnd = rangeEnd.Date < tariff.EndDate.Date ? rangeEnd.Date : tariff.EndDate.Date;

            // Skip if the tariff period is outside the requested range
            if (periodStart > periodEnd)
                continue;

            // Calculate the number of days in the period and the usage within that period
            int days = (periodEnd - periodStart).Days + 1;
            List<MeterReading> periodReadings = [.. readings.Where(r => r.CapturedAt.Date >= periodStart && r.CapturedAt.Date <= periodEnd)];
            decimal usage = periodReadings.Count >= 2 ? periodReadings.Last().Value - periodReadings.First().Value : 0;

            // Calculate the cost for this period and add it to the total
            totalCost += (usage * tariff.UnitRatePence + days * tariff.StandingChargePence) / 100;
        }

        // Round to 2 decimal places for currency representation
        return Math.Round(totalCost, 2);
    }
}