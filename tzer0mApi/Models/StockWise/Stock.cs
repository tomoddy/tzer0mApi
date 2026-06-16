using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tzer0mApi.Models.StockWise;

/// <summary>
/// Represents a physical instance of an item
/// currently in the house.
/// </summary>
[Table("Stock")]
public class Stock
{
    /// <summary>
    /// The unique identifier for the stock entry.
    /// </summary>
    [Key]
    public int StockId { get; set; }

    /// <summary>
    /// The identifier of the item this stock entry refers to.
    /// </summary>
    [Required]
    public int ItemId { get; set; }

    /// <summary>
    /// The identifier of the location this stock entry is stored at.
    /// </summary>
    [Required]
    public int LocationId { get; set; }

    /// <summary>
    /// The quantity of the item at this location.
    /// </summary>
    [Required]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// The expiry date of this stock entry.
    /// </summary>
    public DateOnly? Expiry { get; set; }

    /// <summary>
    /// The date and time this stock entry was added.
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time this item was opened.
    /// Null if the item has not been opened.
    /// </summary>
    public DateTime? OpenedAt { get; set; }

    /// <summary>
    /// The item this stock entry refers to.
    /// </summary>
    [ForeignKey(nameof(ItemId))]
    public Item Item { get; set; } = null!;

    /// <summary>
    /// The location this stock entry is stored at.
    /// </summary>
    [ForeignKey(nameof(LocationId))]
    public Location Location { get; set; } = null!;
}