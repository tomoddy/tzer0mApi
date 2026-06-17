namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents a stock entry with full item and location details.
/// </summary>
public class StockDto
{
    /// <summary>
    /// The unique identifier of the stock entry.
    /// </summary>
    public int StockId { get; set; }

    /// <summary>
    /// The unique identifier of the item.
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// The display name of the item.
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// The barcode of the item.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// The quantity of this stock entry.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The unique identifier of the location.
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// The display name of the location.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the storage category.
    /// </summary>
    public string StorageCategory { get; set; } = string.Empty;

    /// <summary>
    /// The expiry date of this stock entry.
    /// </summary>
    public DateOnly? Expiry { get; set; }

    /// <summary>
    /// The date and time this stock entry was added.
    /// </summary>
    public DateTime AddedAt { get; set; }

    /// <summary>
    /// The date and time this item was opened.
    /// Null if the item has not been opened.
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// Whether this stock entry has been opened.
    /// </summary>
    public bool IsOpened { get; set; }
}