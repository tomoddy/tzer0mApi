namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents the data needed to create a new item, including
/// which storage categories it is allowed in.
/// </summary>
public class CreateItemDto
{
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
    /// The IDs of the storage categories this item is allowed in.
    /// </summary>
    public List<int> AllowedCategoryIds { get; set; } = [];
}