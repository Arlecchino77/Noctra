namespace Noctra.Models;

public class NewsIndexViewModel
{
    public ArticleDto Featured { get; set; } = null!;
    public List<ArticleDto> Articles { get; set; } = new();
}

public class NewsDetailsViewModel
{
    public ArticleDto Article { get; set; } = null!;
    public List<ArticleDto> Others { get; set; } = new();
}

public class ShopIndexViewModel
{
    public List<CollectionDto> Collections { get; set; } = new();
    public ArticleDto? Collaboration { get; set; }
}

public class CollectionViewModel
{
    public CollectionDto Collection { get; set; } = null!;
    public List<ProductDto> Products { get; set; } = new();
}

public class CatalogViewModel
{
    public string Title { get; set; } = "";
    public ProductQuery Query { get; set; } = new();
    public List<ProductDto> Products { get; set; } = new();
    public List<FilterDto> Filters { get; set; } = new();
}

public class ProductViewModel
{
    public ProductDto Product { get; set; } = null!;
    /// <summary>Все размеры из фильтра: недоступные для товара показываются зачёркнутыми.</summary>
    public List<string> AllSizes { get; set; } = new();
}

public class AccountViewModel
{
    public UserDto User { get; set; } = null!;
    public CartDto Cart { get; set; } = null!;
}
