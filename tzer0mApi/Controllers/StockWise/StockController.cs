using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tzer0mApi.Models.StockWise;
using tzer0mApi.Models.StockWise.DTOs;
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
    /// Maps a Stock entity to a StockDto.
    /// </summary>
    private static StockDTO ToDto(Stock stock) => new()
    {
        StockId = stock.StockId,
        ItemId = stock.ItemId,
        ItemName = stock.Item.Name,
        Barcode = stock.Item.Barcode,
        Quantity = stock.Quantity,
        LocationId = stock.LocationId,
        Location = stock.Location.Name,
        StorageCategory = stock.Location.StorageCategory.Name,
        Expiry = stock.Expiry,
        AddedAt = stock.AddedAt,
        OpenedAt = stock.OpenedAt,
        IsOpened = stock.OpenedAt is not null
    };

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

        List<Stock> stock = await query.OrderBy(x => x.Expiry == null).ThenBy(x => x.Expiry).ToListAsync();
        return Ok(stock.Select(ToDto));
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

        return Ok(ToDto(stock));
    }

    /// <summary>
    /// Adds a new stock entry.
    /// Location validity is enforced by the database trigger.
    /// </summary>
    /// <param name="Stock">The stock entry to add.</param>
    [HttpPost]
    public async Task<IActionResult> Add(CreateStockDTO request)
    {
        Stock? existing = await db.Stock.FirstOrDefaultAsync(x =>
            x.ItemId == request.ItemId
            && x.LocationId == request.LocationId
            && x.Expiry == request.Expiry
            && x.OpenedAt == null);

        if (existing is not null)
        {
            existing.Quantity += request.Quantity;
        }
        else
        {
            existing = new Stock
            {
                ItemId = request.ItemId,
                LocationId = request.LocationId,
                Quantity = request.Quantity,
                Expiry = request.Expiry,
                AddedAt = DateTime.UtcNow
            };

            db.Stock.Add(existing);
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest("The selected location is not valid for this item in its current state.");
        }

        Stock created = await db.Stock
            .Include(x => x.Item)
            .Include(x => x.Location)
            .ThenInclude(x => x.StorageCategory)
            .FirstAsync(x => x.StockId == existing.StockId);

        return CreatedAtAction(nameof(GetById), new { id = existing.StockId }, ToDto(created));
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

        Stock? full = await db.Stock
            .Include(x => x.Item)
            .Include(x => x.Location)
            .ThenInclude(x => x.StorageCategory)
            .FirstOrDefaultAsync(x => x.StockId == id);

        return Ok(ToDto(full!));
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
            .Include(x => x.Location)
            .ThenInclude(x => x.StorageCategory)
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

        return Ok(ToDto(stock));
    }

    /// <summary>
    /// Checks out one unit of stock, decrementing its quantity.
    /// If the quantity reaches zero, the stock entry is removed entirely.
    /// </summary>
    /// <param name="id">The stock entry to check out.</param>
    [HttpPatch("{id}/Checkout")]
    public async Task<IActionResult> Checkout(int id)
    {
        Stock? stock = await db.Stock
            .Include(x => x.Item)
            .Include(x => x.Location)
            .ThenInclude(x => x.StorageCategory)
            .FirstOrDefaultAsync(x => x.StockId == id);

        if (stock is null)
            return NotFound();

        if (stock.Quantity <= 0)
            return BadRequest("This stock entry has no quantity remaining.");

        if (stock.Quantity == 1)
        {
            db.Stock.Remove(stock);
            await db.SaveChangesAsync();
            return Ok(new { removed = true });
        }

        stock.Quantity -= 1;
        await db.SaveChangesAsync();

        return Ok(ToDto(stock));
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