namespace tzer0mApi.Models.StockWise.DTOs;

/// <summary>
/// Represents a location within a storage category.
/// </summary>
public class LocationDto
{
    /// <summary>
    /// The unique identifier of the location.
    /// </summary>
    public int LocationId { get; set; }

    /// <summary>
    /// The display name of the location.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the category this location belongs to.
    /// </summary>
    public int CategoryId { get; set; }
}