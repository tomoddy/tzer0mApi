using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tzer0mApi.Models.StockWise;

/// <summary>
/// Represents a top-level storage category
/// e.g. Fridge, Freezer, Cupboard.
/// </summary>
[Table("StorageCategories")]
public class StorageCategory
{
    /// <summary>
    /// The unique identifier for the storage category.
    /// </summary>
    [Key]
    public int CategoryId { get; set; }

    /// <summary>
    /// The display name of the category.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The locations that belong to this category.
    /// </summary>
    public ICollection<Location> Locations { get; set; } = [];

    /// <summary>
    /// The item storage rules that reference this category.
    /// </summary>
    public ICollection<ItemStorageCategory> ItemStorageCategories { get; set; } = [];
}