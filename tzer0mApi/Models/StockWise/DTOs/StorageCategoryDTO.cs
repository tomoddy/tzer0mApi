namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents a storage category with its locations.
/// </summary>
public class StorageCategoryDTO
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
    /// The locations that belong to this category.
    /// </summary>
    public List<LocationDTO> Locations { get; set; } = [];
}