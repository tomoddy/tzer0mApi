using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tzer0mApi.Models.StockWise;
using tzer0mApi.Models.StockWise.DTOs;
using tzer0mApi.Services.StockWise;

namespace tzer0mApi.Controllers.StockWise;

/// <summary>
/// Handles all operations for StockWise storage categories and locations.
/// </summary>
[ApiController]
[Route("StockWise/Storage")]
[Tags("StockWise")]
public class StorageController(StockWiseDbContext db) : ControllerBase
{
    /// <summary>
    /// Maps a StorageCategory entity to a StorageCategoryDto.
    /// </summary>
    private static StorageCategoryDTO ToDto(StorageCategory category) => new()
    {
        CategoryId = category.CategoryId,
        Name = category.Name,
        Locations = [.. category.Locations.Select(ToDto)]
    };

    /// <summary>
    /// Maps a Location entity to a LocationDto.
    /// </summary>
    private static LocationDTO ToDto(Location location) => new()
    {
        LocationId = location.LocationId,
        Name = location.Name,
        CategoryId = location.CategoryId
    };

    /// <summary>
    /// Returns all storage categories with their locations.
    /// </summary>
    [HttpGet("Categories")]
    public async Task<IActionResult> GetAllCategories()
    {
        List<StorageCategory> categories = await db.StorageCategories
            .Include(x => x.Locations)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(categories.Select(ToDto));
    }

    /// <summary>
    /// Returns a single storage category by its ID.
    /// </summary>
    /// <param name="id">The ID of the category to retrieve.</param>
    [HttpGet("Categories/{id:int}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        StorageCategory? category = await db.StorageCategories
            .Include(x => x.Locations)
            .FirstOrDefaultAsync(x => x.CategoryId == id);

        if (category is null)
            return NotFound();

        return Ok(ToDto(category));
    }

    /// <summary>
    /// Adds a new storage category.
    /// </summary>
    /// <param name="category">The category to add.</param>
    [HttpPost("Categories")]
    public async Task<IActionResult> AddCategory(StorageCategory category)
    {
        bool exists = await db.StorageCategories
            .AnyAsync(x => x.Name == category.Name);

        if (exists)
            return Conflict($"A category named '{category.Name}' already exists.");

        db.StorageCategories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryId }, ToDto(category));
    }

    /// <summary>
    /// Updates an existing storage category.
    /// </summary>
    /// <param name="id">The ID of the category to update.</param>
    /// <param name="updated">The updated category data.</param>
    [HttpPut("Categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, StorageCategory updated)
    {
        StorageCategory? category = await db.StorageCategories.FindAsync(id);

        if (category is null)
            return NotFound();

        category.Name = updated.Name;
        await db.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>
    /// Removes a storage category.
    /// Will fail if the category has existing locations.
    /// </summary>
    /// <param name="id">The ID of the category to delete.</param>
    [HttpDelete("Categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        StorageCategory? category = await db.StorageCategories.FindAsync(id);

        if (category is null)
            return NotFound();

        bool hasLocations = await db.Locations.AnyAsync(x => x.CategoryId == id);

        if (hasLocations)
            return BadRequest("Cannot delete a category that has locations assigned to it.");

        db.StorageCategories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Returns all locations within a given category.
    /// </summary>
    /// <param name="categoryId">The ID of the category to retrieve locations for.</param>
    [HttpGet("Categories/{categoryId:int}/Locations")]
    public async Task<IActionResult> GetLocationsByCategory(int categoryId)
    {
        bool categoryExists = await db.StorageCategories.AnyAsync(x => x.CategoryId == categoryId);

        if (!categoryExists)
            return NotFound();

        List<Location> locations = await db.Locations
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(locations.Select(ToDto));
    }

    /// <summary>
    /// Returns a single location by its ID.
    /// </summary>
    /// <param name="id">The ID of the location to retrieve.</param>
    [HttpGet("Locations/{id:int}")]
    public async Task<IActionResult> GetLocationById(int id)
    {
        Location? location = await db.Locations
            .Include(x => x.StorageCategory)
            .FirstOrDefaultAsync(x => x.LocationId == id);

        if (location is null)
            return NotFound();

        return Ok(ToDto(location));
    }

    /// <summary>
    /// Adds a new location under a storage category.
    /// </summary>
    /// <param name="location">The location to add.</param>
    [HttpPost("Locations")]
    public async Task<IActionResult> AddLocation(Location location)
    {
        bool categoryExists = await db.StorageCategories
            .AnyAsync(x => x.CategoryId == location.CategoryId);

        if (!categoryExists)
            return BadRequest("The specified category does not exist.");

        bool exists = await db.Locations
            .AnyAsync(x => x.CategoryId == location.CategoryId && x.Name == location.Name);

        if (exists)
            return Conflict($"A location named '{location.Name}' already exists in this category.");

        db.Locations.Add(location);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLocationById), new { id = location.LocationId }, ToDto(location));
    }

    /// <summary>
    /// Updates an existing location.
    /// </summary>
    /// <param name="id">The ID of the location to update.</param>
    /// <param name="updated">The updated location data.</param>
    [HttpPut("Locations/{id:int}")]
    public async Task<IActionResult> UpdateLocation(int id, Location updated)
    {
        Location? location = await db.Locations.FindAsync(id);

        if (location is null)
            return NotFound();

        location.Name = updated.Name;
        await db.SaveChangesAsync();
        return Ok(ToDto(location));
    }

    /// <summary>
    /// Removes a location.
    /// Will fail if the location has existing stock entries.
    /// </summary>
    /// <param name="id">The ID of the location to delete.</param>
    [HttpDelete("Locations/{id:int}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        Location? location = await db.Locations.FindAsync(id);

        if (location is null)
            return NotFound();

        bool hasStock = await db.Stock.AnyAsync(x => x.LocationId == id);

        if (hasStock)
            return BadRequest("Cannot delete a location that has stock assigned to it.");

        db.Locations.Remove(location);
        await db.SaveChangesAsync();
        return NoContent();
    }
}