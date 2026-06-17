namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents a storage category allowed for an item,
/// including the opened/unopened rules.
/// </summary>
public class ItemCategoryDto
{
    /// <summary>
    /// The unique identifier of the category.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// The display name of the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this category is allowed when the item is unopened.
    /// </summary>
    public bool AllowedWhenUnopened { get; set; }

    /// <summary>
    /// Whether this category is allowed when the item is opened.
    /// </summary>
    public bool AllowedWhenOpened { get; set; }
}