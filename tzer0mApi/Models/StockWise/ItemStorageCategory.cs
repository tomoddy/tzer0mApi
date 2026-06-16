using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tzer0mApi.Models.StockWise;

/// <summary>
/// Represents the allowed storage categories for an item,
/// including whether the rule applies before or after opening.
/// </summary>
[Table("ItemStorageCategories")]
public class ItemStorageCategory
{
    /// <summary>
    /// The identifier of the item this rule applies to.
    /// </summary>
    [Required]
    public int ItemId { get; set; }

    /// <summary>
    /// The identifier of the storage category this rule applies to.
    /// </summary>
    [Required]
    public int CategoryId { get; set; }

    /// <summary>
    /// Whether this storage category is allowed when the item is unopened.
    /// </summary>
    public bool AllowedWhenUnopened { get; set; } = true;

    /// <summary>
    /// Whether this storage category is allowed when the item has been opened.
    /// </summary>
    public bool AllowedWhenOpened { get; set; } = true;

    /// <summary>
    /// The item this rule applies to.
    /// </summary>
    [ForeignKey(nameof(ItemId))]
    public Item Item { get; set; } = null!;

    /// <summary>
    /// The storage category this rule applies to.
    /// </summary>
    [ForeignKey(nameof(CategoryId))]
    public StorageCategory StorageCategory { get; set; } = null!;
}