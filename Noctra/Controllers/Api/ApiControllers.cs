using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Noctra.Data;
using Noctra.Models;
using Noctra.Services;

namespace Noctra.Controllers.Api;

/// <summary>Регистрация, вход и выход.</summary>
[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AccountService _accounts;

    public AuthApiController(AccountService accounts) => _accounts = accounts;

    /// <summary>Регистрация нового аккаунта (после неё пользователь сразу входит).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest req)
    {
        var passwordError = AccountService.ValidatePassword(req.Password);
        if (passwordError != null) return BadRequest(new { error = passwordError });

        var (user, error) = await _accounts.RegisterAsync(req.Name, req.Email, req.Phone, req.Password);
        if (user == null) return Conflict(new { error });

        await AccountService.SignInAsync(HttpContext, user);
        return new UserDto(user.Id, user.Name, user.Email, user.Phone, user.CreatedAt);
    }

    /// <summary>Вход по имени пользователя, почте и паролю.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginRequest req)
    {
        var user = await _accounts.CheckCredentialsAsync(req.Name, req.Email, req.Password);
        if (user == null) return Unauthorized(new { error = "Wrong username, email or password" });

        await AccountService.SignInAsync(HttpContext, user);
        return new UserDto(user.Id, user.Name, user.Email, user.Phone, user.CreatedAt);
    }

    /// <summary>Выход из аккаунта.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await AccountService.SignOutAsync(HttpContext);
        return NoContent();
    }

    /// <summary>Текущий пользователь.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me([FromServices] AppDbContext db)
    {
        var id = User.GetUserId();
        var user = await db.Users.FindAsync(id);
        if (user == null) return Unauthorized();
        return new UserDto(user.Id, user.Name, user.Email, user.Phone, user.CreatedAt);
    }
}

/// <summary>Каталог: категории, коллекции, фильтры, товары.</summary>
[ApiController]
[Route("api")]
public class CatalogApiController : ControllerBase
{
    private readonly CatalogService _catalog;

    public CatalogApiController(CatalogService catalog) => _catalog = catalog;

    /// <summary>Список категорий.</summary>
    [HttpGet("categories")]
    public Task<List<CategoryDto>> Categories() => _catalog.GetCategoriesAsync();

    /// <summary>Список коллекций.</summary>
    [HttpGet("collections")]
    public Task<List<CollectionDto>> Collections() => _catalog.GetCollectionsAsync();

    /// <summary>Коллекция по slug.</summary>
    [HttpGet("collections/{slug}")]
    public async Task<ActionResult<CollectionDto>> Collection(string slug) =>
        await _catalog.GetCollectionAsync(slug) is { } c ? c : NotFound();

    /// <summary>Фильтры каталога и их значения.</summary>
    [HttpGet("filters")]
    public Task<List<FilterDto>> Filters() => _catalog.GetFiltersAsync();

    /// <summary>Товары с поиском, фильтрами и сортировкой.</summary>
    [HttpGet("products")]
    public Task<List<ProductDto>> Products([FromQuery] ProductQuery query) => _catalog.GetProductsAsync(query);

    /// <summary>Товар по id.</summary>
    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<ProductDto>> Product(int id) =>
        await _catalog.GetProductAsync(id) is { } p ? p : NotFound();
}

/// <summary>Фото товаров, хранящиеся в базе данных.</summary>
[ApiController]
[Route("api/images")]
public class ImagesApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public ImagesApiController(AppDbContext db) => _db = db;

    /// <summary>Отдаёт фото товара из базы данных.</summary>
    [HttpGet("{id:int}")]
    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> Get(int id)
    {
        var image = await _db.ProductImages.AsNoTracking()
            .Where(i => i.Id == id)
            .Select(i => new { i.Data, i.ContentType })
            .FirstOrDefaultAsync();
        return image == null ? NotFound() : File(image.Data, image.ContentType);
    }
}

/// <summary>Корзина текущего пользователя.</summary>
[ApiController]
[Authorize]
[Route("api/cart")]
public class CartApiController : ControllerBase
{
    private readonly CartService _cart;

    public CartApiController(CartService cart) => _cart = cart;

    private int UserId => User.GetUserId()!.Value;

    /// <summary>Содержимое корзины.</summary>
    [HttpGet]
    public Task<CartDto> Get() => _cart.GetCartAsync(UserId);

    /// <summary>Добавить товар в корзину.</summary>
    [HttpPost]
    public async Task<ActionResult<CartDto>> Add(AddToCartRequest req)
    {
        var error = await _cart.AddAsync(UserId, req);
        if (error != null) return BadRequest(new { error });
        return await _cart.GetCartAsync(UserId);
    }

    /// <summary>Изменить количество.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CartDto>> Update(int id, UpdateCartRequest req)
    {
        if (!await _cart.UpdateAsync(UserId, id, req.Quantity)) return NotFound();
        return await _cart.GetCartAsync(UserId);
    }

    /// <summary>Удалить позицию из корзины.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<CartDto>> Remove(int id)
    {
        if (!await _cart.RemoveAsync(UserId, id)) return NotFound();
        return await _cart.GetCartAsync(UserId);
    }

    /// <summary>Очистить корзину.</summary>
    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        await _cart.ClearAsync(UserId);
        return NoContent();
    }

    /// <summary>Оформить заказ (корзина очищается).</summary>
    [HttpPost("order")]
    public async Task<IActionResult> Order()
    {
        var cart = await _cart.GetCartAsync(UserId);
        if (cart.Count == 0) return BadRequest(new { error = "Cart is empty" });
        await _cart.ClearAsync(UserId);
        return Ok(new { message = "Order placed", total = cart.Total });
    }
}

/// <summary>Новостная лента.</summary>
[ApiController]
[Route("api/news")]
public class NewsApiController : ControllerBase
{
    private readonly CatalogService _catalog;

    public NewsApiController(CatalogService catalog) => _catalog = catalog;

    /// <summary>Все статьи, новые сначала.</summary>
    [HttpGet]
    public Task<List<ArticleDto>> List() => _catalog.GetArticlesAsync();

    /// <summary>Статья по id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticleDto>> Get(int id) =>
        await _catalog.GetArticleAsync(id) is { } a ? a : NotFound();
}
