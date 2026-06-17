using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tzer0mApi.Models.StockWise;

/// <summary>
/// Represents a product in the catalogue.
/// One item can have many stock entries.
/// </summary>
[Table("Items")]
public class Item
{
    /// <summary>
    /// The unique identifier for the item.
    /// </summary>
    [Key]
    public int ItemId { get; set; }

    /// <summary>
    /// The barcode of the item as scanned by the app.
    /// </summary>
    [MaxLength(50)]
    public string? Barcode { get; set; }

    /// <summary>
    /// The display name of the item.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// An optional URL pointing to an image of the item.
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The date and time the item was added to the catalogue.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The storage category rules that apply to this item.
    /// </summary>
    public ICollection<ItemStorageCategory> ItemStorageCategories { get; set; } = [];

    /// <summary>
    /// The stock entries for this item.
    /// </summary>
    public ICollection<Stock> Stock { get; set; } = [];

    /// <summary>
    /// Whether this item has a distinct "opened" state that affects
    /// storage location and expiry once opened.
    /// </summary>
    public bool IsOpenable { get; set; }
}