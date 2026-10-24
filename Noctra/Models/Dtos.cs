using System.ComponentModel.DataAnnotations;

namespace Noctra.Models;

// Объекты, которые API отдаёт и принимает

public record CategoryDto(int Id, string Name, string Slug, int ProductCount);

public record CollectionDto(int Id, string Name, string Slug, string Description, string CoverUrl, int ProductCount);

public record FilterOptionDto(int Id, string Value);

public record FilterDto(int Id, string Name, string Code, List<FilterOptionDto> Options);

public record ProductDto(
    int Id,
    string Name,
    string Description,
    string Material,
    decimal Price,
    bool IsNew,
    CategoryDto? Category,
    string? CollectionName,
    string? CollectionSlug,
    List<string> ImageUrls,
    Dictionary<string, List<string>> Filters);

public record CartItemDto(int Id, int ProductId, string Name, string Size, int Quantity, decimal Price, decimal Total, string? ImageUrl);

public record CartDto(List<CartItemDto> Items, int Count, decimal Total);

public record ArticleDto(int Id, string Title, string Subtitle, string Tag, string Location, string Body, string ImageUrl, DateTime PublishedAt);

public record UserDto(int Id, string Name, string Email, string Phone, DateTime CreatedAt);

/// <summary>Параметры поиска и фильтрации каталога.</summary>
public class ProductQuery
{
    /// <summary>Поиск по названию и описанию.</summary>
    public string? Search { get; set; }
    /// <summary>Slug категории (jackets, coats...).</summary>
    public string? Category { get; set; }
    /// <summary>Slug коллекции (spring, autumn...).</summary>
    public string? Collection { get; set; }
    /// <summary>Размеры: ?size=S&amp;size=M (или через запятую).</summary>
    public List<string> Size { get; set; } = new();
    /// <summary>Цвета: ?color=BLACK&amp;color=RED.</summary>
    public List<string> Color { get; set; } = new();
    /// <summary>Материалы: ?material=LEATHER.</summary>
    public List<string> Material { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    /// <summary>Сортировка: new, price_asc, price_desc, name.</summary>
    public string? Sort { get; set; }
}

public class RegisterRequest
{
    [Required, StringLength(60, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, Phone, StringLength(20, MinimumLength = 6)]
    public string Phone { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = "";
}

public class LoginRequest
{
    [Required, StringLength(60)]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

public class AddToCartRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required, StringLength(10)]
    public string Size { get; set; } = "";

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartRequest
{
    [Range(1, 20)]
    public int Quantity { get; set; }
}
