using System.ComponentModel.DataAnnotations;

namespace Noctra.Models;

/// <summary>Аккаунт покупателя.</summary>
public class User
{
    public int Id { get; set; }

    [MaxLength(60)]
    public string Name { get; set; } = "";

    [MaxLength(120)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string Phone { get; set; } = "";

    /// <summary>Хэш пароля PBKDF2 (соль + хэш), пароль в открытом виде не хранится.</summary>
    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CartItem> CartItems { get; set; } = new();
}

/// <summary>Категория товаров (куртки, пальто, свитеры...).</summary>
public class Category
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Name { get; set; } = "";

    [MaxLength(40)]
    public string Slug { get; set; } = "";

    public int SortOrder { get; set; }

    public List<Product> Products { get; set; } = new();
}

/// <summary>Коллекция (весна, лето, осень, Токио, украшения).</summary>
public class Collection
{
    public int Id { get; set; }

    [MaxLength(60)]
    public string Name { get; set; } = "";

    [MaxLength(40)]
    public string Slug { get; set; } = "";

    [MaxLength(400)]
    public string Description { get; set; } = "";

    /// <summary>Путь к обложке коллекции в wwwroot.</summary>
    [MaxLength(200)]
    public string CoverPath { get; set; } = "";

    public int SortOrder { get; set; }

    public List<Product> Products { get; set; } = new();
}

public class Product
{
    public int Id { get; set; }

    [MaxLength(80)]
    public string Name { get; set; } = "";

    [MaxLength(1000)]
    public string Description { get; set; } = "";

    [MaxLength(300)]
    public string Material { get; set; } = "";

    public decimal Price { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public int? CollectionId { get; set; }
    public Collection? Collection { get; set; }

    public bool IsNew { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ProductImage> Images { get; set; } = new();

    public List<FilterOption> FilterOptions { get; set; } = new();
}

/// <summary>Фото товара. Само изображение хранится в базе данных (BLOB).</summary>
public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [MaxLength(120)]
    public string FileName { get; set; } = "";

    [MaxLength(40)]
    public string ContentType { get; set; } = "image/jpeg";

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public int SortOrder { get; set; }
}

/// <summary>Фильтр каталога (размер, цвет, цена...).</summary>
public class Filter
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string Name { get; set; } = "";

    /// <summary>Код фильтра для строки запроса: size, color, material.</summary>
    [MaxLength(20)]
    public string Code { get; set; } = "";

    public int SortOrder { get; set; }

    public List<FilterOption> Options { get; set; } = new();
}

/// <summary>Значение фильтра (S, M, L / BLACK, WHITE...).</summary>
public class FilterOption
{
    public int Id { get; set; }

    public int FilterId { get; set; }
    public Filter? Filter { get; set; }

    [MaxLength(40)]
    public string Value { get; set; } = "";

    public int SortOrder { get; set; }

    public List<Product> Products { get; set; } = new();
}

/// <summary>Позиция в корзине пользователя.</summary>
public class CartItem
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [MaxLength(10)]
    public string Size { get; set; } = "";

    public int Quantity { get; set; } = 1;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Статья новостной ленты.</summary>
public class Article
{
    public int Id { get; set; }

    [MaxLength(120)]
    public string Title { get; set; } = "";

    [MaxLength(120)]
    public string Subtitle { get; set; } = "";

    /// <summary>Тип события для подписи на фото: FASHION SHOW, PHOTO SHOOT...</summary>
    [MaxLength(40)]
    public string Tag { get; set; } = "";

    /// <summary>Место: IN COPENHAGEN, IN MOSCOW...</summary>
    [MaxLength(60)]
    public string Location { get; set; } = "";

    [MaxLength(4000)]
    public string Body { get; set; } = "";

    [MaxLength(200)]
    public string ImagePath { get; set; } = "";

    public DateTime PublishedAt { get; set; }
}
