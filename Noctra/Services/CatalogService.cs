using Microsoft.EntityFrameworkCore;
using Noctra.Data;
using Noctra.Models;

namespace Noctra.Services;

public class CatalogService
{
    private readonly AppDbContext _db;

    public CatalogService(AppDbContext db) => _db = db;

    public static string ImageUrl(int imageId) => $"/api/images/{imageId}";

    public async Task<List<CategoryDto>> GetCategoriesAsync() =>
        await _db.Categories.OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug, c.Products.Count))
            .ToListAsync();

    public async Task<List<CollectionDto>> GetCollectionsAsync() =>
        await _db.Collections.OrderBy(c => c.SortOrder)
            .Select(c => new CollectionDto(c.Id, c.Name, c.Slug, c.Description, c.CoverPath, c.Products.Count))
            .ToListAsync();

    public async Task<CollectionDto?> GetCollectionAsync(string slug) =>
        await _db.Collections.Where(c => c.Slug == slug)
            .Select(c => new CollectionDto(c.Id, c.Name, c.Slug, c.Description, c.CoverPath, c.Products.Count))
            .FirstOrDefaultAsync();

    public async Task<List<FilterDto>> GetFiltersAsync() =>
        await _db.Filters.OrderBy(f => f.SortOrder)
            .Select(f => new FilterDto(f.Id, f.Name, f.Code,
                f.Options.OrderBy(o => o.SortOrder).Select(o => new FilterOptionDto(o.Id, o.Value)).ToList()))
            .ToListAsync();

    public async Task<List<ProductDto>> GetProductsAsync(ProductQuery q)
    {
        var query = WithIncludes();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Category!.Name.ToLower().Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(q.Category))
            query = query.Where(p => p.Category!.Slug == q.Category);
        if (!string.IsNullOrWhiteSpace(q.Collection))
            query = query.Where(p => p.Collection != null && p.Collection.Slug == q.Collection);
        if (q.MinPrice.HasValue)
            query = query.Where(p => p.Price >= q.MinPrice.Value);
        if (q.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= q.MaxPrice.Value);

        // Внутри одного фильтра значения объединяются через ИЛИ, разные фильтры — через И
        foreach (var (code, raw) in new[] { ("size", q.Size), ("color", q.Color), ("material", q.Material) })
        {
            var values = Split(raw);
            if (values.Count == 0) continue;
            query = query.Where(p => p.FilterOptions.Any(o => o.Filter!.Code == code && values.Contains(o.Value)));
        }

        query = q.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.IsNew).ThenBy(p => p.Id),
        };

        var products = await query.ToListAsync();
        var images = await ImageIdsAsync(products.Select(p => p.Id).ToList());
        return products.Select(p => ToDto(p, images.GetValueOrDefault(p.Id) ?? new())).ToList();
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var p = await WithIncludes().FirstOrDefaultAsync(p => p.Id == id);
        if (p == null) return null;
        var images = await ImageIdsAsync(new List<int> { p.Id });
        return ToDto(p, images.GetValueOrDefault(p.Id) ?? new());
    }

    public async Task<List<ArticleDto>> GetArticlesAsync() =>
        await _db.Articles.OrderBy(a => a.Id)
            .Select(a => new ArticleDto(a.Id, a.Title, a.Subtitle, a.Tag, a.Location, a.Body, a.ImagePath, a.PublishedAt))
            .ToListAsync();

    public async Task<ArticleDto?> GetArticleAsync(int id) =>
        await _db.Articles.Where(a => a.Id == id)
            .Select(a => new ArticleDto(a.Id, a.Title, a.Subtitle, a.Tag, a.Location, a.Body, a.ImagePath, a.PublishedAt))
            .FirstOrDefaultAsync();

    private IQueryable<Product> WithIncludes() =>
        _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Collection)
            .Include(p => p.FilterOptions).ThenInclude(o => o.Filter)
            .AsSplitQuery();

    // Сами байты фото не загружаем — только их Id для ссылок /api/images/{id}
    private async Task<Dictionary<int, List<int>>> ImageIdsAsync(List<int> productIds) =>
        (await _db.ProductImages.Where(i => productIds.Contains(i.ProductId))
            .OrderBy(i => i.SortOrder)
            .Select(i => new { i.Id, i.ProductId })
            .ToListAsync())
        .GroupBy(i => i.ProductId)
        .ToDictionary(g => g.Key, g => g.Select(i => i.Id).ToList());

    private static ProductDto ToDto(Product p, List<int> imageIds)
    {
        var filters = p.FilterOptions
            .OrderBy(o => o.SortOrder)
            .GroupBy(o => o.Filter!.Code)
            .ToDictionary(g => g.Key, g => g.Select(o => o.Value).ToList());

        return new ProductDto(
            p.Id, p.Name, p.Description, p.Material, p.Price, p.IsNew,
            p.Category == null ? null : new CategoryDto(p.Category.Id, p.Category.Name, p.Category.Slug, 0),
            p.Collection?.Name, p.Collection?.Slug,
            imageIds.Select(ImageUrl).ToList(),
            filters);
    }

    public static List<string> Split(IEnumerable<string>? raw) =>
        (raw ?? Enumerable.Empty<string>())
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(v => v.ToUpperInvariant()).Distinct().ToList();
}
