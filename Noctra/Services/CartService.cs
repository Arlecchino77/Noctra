using Microsoft.EntityFrameworkCore;
using Noctra.Data;
using Noctra.Models;

namespace Noctra.Services;

public class CartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public async Task<CartDto> GetCartAsync(int userId)
    {
        var rows = await _db.CartItems.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.AddedAt)
            .Select(c => new
            {
                c.Id, c.ProductId, c.Product!.Name, c.Size, c.Quantity, c.Product.Price,
                ImageId = c.Product.Images.OrderBy(i => i.SortOrder).Select(i => (int?)i.Id).FirstOrDefault(),
            })
            .ToListAsync();

        var items = rows.Select(r => new CartItemDto(r.Id, r.ProductId, r.Name, r.Size, r.Quantity, r.Price,
            r.Price * r.Quantity, r.ImageId.HasValue ? CatalogService.ImageUrl(r.ImageId.Value) : null)).ToList();

        return new CartDto(items, items.Sum(i => i.Quantity), items.Sum(i => i.Total));
    }

    public async Task<int> CountAsync(int userId) =>
        await _db.CartItems.Where(c => c.UserId == userId).SumAsync(c => (int?)c.Quantity) ?? 0;

    public async Task<string?> AddAsync(int userId, AddToCartRequest req)
    {
        var size = req.Size.Trim().ToUpperInvariant();
        var product = await _db.Products.Include(p => p.FilterOptions).ThenInclude(o => o.Filter)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId);
        if (product == null) return "Product not found";

        var sizes = product.FilterOptions.Where(o => o.Filter!.Code == "size").Select(o => o.Value).ToList();
        if (!sizes.Contains(size)) return "This size is not available";

        var item = await _db.CartItems.FirstOrDefaultAsync(c =>
            c.UserId == userId && c.ProductId == req.ProductId && c.Size == size);
        if (item == null)
            _db.CartItems.Add(new CartItem { UserId = userId, ProductId = req.ProductId, Size = size, Quantity = req.Quantity });
        else
            item.Quantity = Math.Min(20, item.Quantity + req.Quantity);

        await _db.SaveChangesAsync();
        return null;
    }

    public async Task<bool> UpdateAsync(int userId, int itemId, int quantity)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);
        if (item == null) return false;
        item.Quantity = quantity;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveAsync(int userId, int itemId)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);
        if (item == null) return false;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task ClearAsync(int userId) =>
        await _db.CartItems.Where(c => c.UserId == userId).ExecuteDeleteAsync();
}
