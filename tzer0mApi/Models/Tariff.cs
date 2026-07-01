namespace tzer0mApi.Models;

/// <summary>
/// Represents a tariff period with a fixed unit rate and standing charge,
/// valid between StartDate and EndDate inclusive.
/// </summary>
public class Tariff
{
    /// <summary>
    /// First day this tariff applies from.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Last day this tariff applies to.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Cost per kWh in pence.
    /// </summary>
    public decimal UnitRatePence { get; set; }

    /// <summary>
    /// Daily standing charge in pence.
    /// </summary>
    public decimal StandingChargePence { get; set; }
}