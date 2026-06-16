using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace tzer0mApi.Models.StockWise;

/// <summary>
/// Represents a specific physical storage location
/// e.g. Cupboard 1 Shelf 1, Fridge - Top Shelf.
/// </summary>
[Table("Locations")]
public class Location
{
    /// <summary>
    /// The unique identifier for the location.
    /// </summary>
    [Key]
    public int LocationId { get; set; }

    /// <summary>
    /// The category this location belongs to
    /// e.g. Cupboard, Fridge, Freezer.
    /// </summary>
    [Required]
    public int CategoryId { get; set; }

    /// <summary>
    /// The display name of the location.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The storage category this location belongs to.
    /// </summary>
    [ForeignKey(nameof(CategoryId))]
    public StorageCategory StorageCategory { get; set; } = null!;

    /// <summary>
    /// The stock entries stored at this location.
    /// </summary>
    public ICollection<Stock> Stock { get; set; } = [];
}