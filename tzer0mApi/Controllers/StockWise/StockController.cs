using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tzer0mApi.Models.StockWise;
using tzer0mApi.Services.StockWise;

namespace tzer0mApi.Controllers.StockWise;

/// <summary>
/// Handles all operations for StockWise stock entries.
/// </summary>
[ApiController]
[Route("StockWise/Stock")]
[Tags("StockWise")]
public class StockController(StockWiseDbContext db) : ControllerBase
{
    /// <summary>
    /// Returns all stock entries, optionally filtered by item, location or expiry date.
    /// </summary>
    /// <param name="itemId">Optional item ID to filter by.</param>
    /// <param name="locationId">Optional location ID to filter by.</param>
    /// <param name="expiresBefore">Optional date to return only stock expiring before this date.</param>
    [HttpGet]
    public async Task<IActionResult> GetAll(int? itemId, int? locationId, DateOnly? expiresBefore)
    {
        IQueryable<Stock> query = db.Stock
            .Include(x => x.Item)
            .Include(x => x.Location)
            .ThenInclude(x => x.StorageCategory);

        if (itemId.HasValue)
            query = query.Where(x => x.ItemId == itemId);

        if (locationId.HasValue)
            query = query.Where(x => x.LocationId == locationId);

        if (expiresBefore.HasValue)
            query = query.Where(x => x.Expiry.HasValue && x.Expiry <= expiresBefore);

        return Ok(await query.OrderBy(x => x.Expiry).ToListAsync());
    }

    /// <summary>
    /// Returns a single stock entry by its ID.
    /// </summary>
    /// <param name="id">The ID of the stock entry to retrieve.</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        Stock? stock = await db.Stock
            .Include(x => x.Item)
            .Include(x => x.Location)
            .ThenInclude(x => x.StorageCategory)
            .FirstOrDefaultAsync(x => x.StockId == id);

        if (stock is null)
            return NotFound();

        return Ok(stock);
    }

    /// <summary>
    /// Adds a new stock entry.
    /// Location validity is enforced by the database trigger.
    /// </summary>
    /// <param name="stock">The stock entry to add.</param>
    [HttpPost]
    public async Task<IActionResult> Add(Stock stock)
    {
        stock.AddedAt = DateTime.UtcNow;
        db.Stock.Add(stock);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("not a valid storage option") == true)
        {
            return BadRequest("The selected location is not valid for this item in its current state.");
        }

        return CreatedAtAction(nameof(GetById), new { id = stock.StockId }, stock);
    }

    /// <summary>
    /// Updates an existing stock entry.
    /// Location validity is enforced by the database trigger.
    /// </summary>
    /// <param name="id">The ID of the stock entry to update.</param>
    /// <param name="updated">The updated stock data.</param>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, Stock updated)
    {
        Stock? stock = await db.Stock.FindAsync(id);

        if (stock is null)
            return NotFound();

        stock.ItemId = updated.ItemId;
        stock.LocationId = updated.LocationId;
        stock.Quantity = updated.Quantity;
        stock.Expiry = updated.Expiry;
        stock.OpenedAt = updated.OpenedAt;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("not a valid storage option") == true)
        {
            return BadRequest("The selected location is not valid for this item in its current state.");
        }

        return Ok(stock);
    }

    /// <summary>
    /// Marks a stock entry as opened by setting OpenedAt to now.
    /// If the item's open storage rules differ, the location may need updating.
    /// </summary>
    /// <param name="id">The ID of the stock entry to mark as opened.</param>
    [HttpPatch("{id:int}/Open")]
    public async Task<IActionResult> Open(int id)
    {
        Stock? stock = await db.Stock
            .Include(x => x.Item)
            .ThenInclude(x => x.ItemStorageCategories)
            .FirstOrDefaultAsync(x => x.StockId == id);

        if (stock is null)
            return NotFound();

        stock.OpenedAt = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("not a valid storage option") == true)
        {
            return BadRequest("This item must be moved to a valid location before it can be marked as opened.");
        }

        return Ok(stock);
    }

    /// <summary>
    /// Removes a stock entry.
    /// </summary>
    /// <param name="id">The ID of the stock entry to delete.</param>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        Stock? stock = await db.Stock.FindAsync(id);

        if (stock is null)
            return NotFound();

        db.Stock.Remove(stock);
        await db.SaveChangesAsync();
        return NoContent();
    }
}