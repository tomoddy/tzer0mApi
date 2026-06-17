namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents the data needed to open a sealed stock entry.
/// </summary>
public class OpenStockDTO
{
    /// <summary>
    /// The new location for the opened unit.
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// The new expiry date for the opened unit.
    /// </summary>
    public DateOnly? Expiry { get; set; }
}