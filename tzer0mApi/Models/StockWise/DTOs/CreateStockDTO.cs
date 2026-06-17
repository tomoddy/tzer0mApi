namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents the data needed to create a new stock entry.
/// </summary>
public class CreateStockDto
{
    /// <summary>
    /// The identifier of the item this stock entry refers to.
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// The identifier of the location this stock entry is stored at.
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// The quantity of the item at this location.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The expiry date of this stock entry, if known.
    /// </summary>
    public DateOnly? Expiry { get; set; }
}