namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents an item in the catalogue with its allowed storage categories.
/// </summary>
public class ItemDTO
{
    /// <summary>
    /// The unique identifier of the item.
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// The barcode of the item.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// The display name of the item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// An optional URL pointing to an image of the item.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The date and time the item was added to the catalogue.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The storage categories this item is allowed in.
    /// </summary>
    public List<ItemCategoryDTO> AllowedCategories { get; set; } = [];

    /// <summary>
    /// Whether this item has a distinct "opened" state that affects
    /// storage location and expiry once opened.
    /// </summary>
    public bool IsOpenable { get; set; }
}