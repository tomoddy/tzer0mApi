using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tzer0mApi.Models.StockWise;
using tzer0mApi.Services.StockWise;

namespace tzer0mApi.Controllers.StockWise;

/// <summary>
/// Handles all operations for the StockWise item catalogue.
/// </summary>
[ApiController]
[Route("StockWise/Items")]
[Tags("StockWise")]
public class ItemsController(StockWiseDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns all items in the catalogue.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        List<Item> items = await db.Items
            .Include(x => x.ItemStorageCategories)
            .ThenInclude(x => x.StorageCategory)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Returns a single item by its ID.
    /// </summary>
    /// <param name="id">The ID of the item to retrieve.</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        Item? item = await db.Items
            .Include(x => x.ItemStorageCategories)
            .ThenInclude(x => x.StorageCategory)
            .FirstOrDefaultAsync(x => x.ItemId == id);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Returns a single item by its barcode.
    /// Used by the MAUI app immediately after a barcode scan.
    /// </summary>
    /// <param name="barcode">The barcode to look up.</param>
    [HttpGet("Barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(string barcode)
    {
        Item? item = await db.Items
            .Include(x => x.ItemStorageCategories)
            .ThenInclude(x => x.StorageCategory)
            .FirstOrDefaultAsync(x => x.Barcode == barcode);

        if (item is null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>
    /// Adds a new item to the catalogue.
    /// Returns the existing item if the barcode already exists.
    /// </summary>
    /// <param name="item">The item to add.</param>
    [HttpPost]
    public async Task<IActionResult> Add(Item item)
    {
        if (item.Barcode is not null)
        {
            Item? existing = await db.Items
                .Include(x => x.ItemStorageCategories)
                .ThenInclude(x => x.StorageCategory)
                .FirstOrDefaultAsync(x => x.Barcode == item.Barcode);

            if (existing is not null)
                return Conflict(existing);
        }

        item.CreatedAt = DateTime.UtcNow;
        db.Items.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.ItemId }, item);
    }

    /// <summary>
    /// Updates an existing item in the catalogue.
    /// </summary>
    /// <param name="id">The ID of the item to update.</param>
    /// <param name="updated">The updated item data.</param>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Item updated)
    {
        Item? item = await db.Items.FindAsync(id);

        if (item is null)
            return NotFound();

        item.Name = updated.Name;
        item.Barcode = updated.Barcode;
        item.ImageUrl = updated.ImageUrl;

        await db.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>
    /// Removes an item from the catalogue.
    /// Will fail if the item has existing stock entries.
    /// </summary>
    /// <param name="id">The ID of the item to delete.</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        Item? item = await db.Items.FindAsync(id);

        if (item is null)
            return NotFound();

        db.Items.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}