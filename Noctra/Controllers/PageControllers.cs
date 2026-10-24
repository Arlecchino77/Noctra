using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Noctra.Data;
using Noctra.Models;
using Noctra.Services;

namespace Noctra.Controllers;

/// <summary>Новостная лента — стартовая страница.</summary>
public class NewsController : Controller
{
    private readonly CatalogService _catalog;

    public NewsController(CatalogService catalog) => _catalog = catalog;

    /// <summary>Лента: выбранная статья показывается крупно, остальные — карточками справа.</summary>
    public async Task<IActionResult> Index(int? id)
    {
        var articles = await _catalog.GetArticlesAsync();
        if (articles.Count == 0) return View("Empty");
        var featured = articles.FirstOrDefault(a => a.Id == id) ?? articles[0];
        return View(new NewsIndexViewModel { Featured = featured, Articles = articles });
    }

    public async Task<IActionResult> Details(int id)
    {
        var article = await _catalog.GetArticleAsync(id);
        if (article == null) return NotFound();
        var others = (await _catalog.GetArticlesAsync()).Where(a => a.Id != id).ToList();
        return View(new NewsDetailsViewModel { Article = article, Others = others });
    }
}

/// <summary>Магазин: подборка коллекций, коллекция, каталог с фильтрами, товар.</summary>
public class ShopController : Controller
{
    private readonly CatalogService _catalog;

    public ShopController(CatalogService catalog) => _catalog = catalog;

    public async Task<IActionResult> Index()
    {
        var articles = await _catalog.GetArticlesAsync();
        return View(new ShopIndexViewModel
        {
            Collections = await _catalog.GetCollectionsAsync(),
            Collaboration = articles.FirstOrDefault(a => a.Subtitle == "COLLABORATION"),
        });
    }

    public async Task<IActionResult> Collection(string id)
    {
        var collection = await _catalog.GetCollectionAsync(id);
        if (collection == null) return NotFound();
        // Порядок как на макете: товары расставляются по плиткам мозаики в порядке добавления
        var products = (await _catalog.GetProductsAsync(new ProductQuery { Collection = id }))
            .OrderBy(p => p.Id).ToList();
        return View(new CollectionViewModel { Collection = collection, Products = products });
    }

    public async Task<IActionResult> Catalog([FromQuery] ProductQuery query)
    {
        var categories = await _catalog.GetCategoriesAsync();
        var title = categories.FirstOrDefault(c => c.Slug == query.Category)?.Name
                    ?? (string.IsNullOrWhiteSpace(query.Search) ? "ALL ITEMS" : $"SEARCH: {query.Search}");
        return View(new CatalogViewModel
        {
            Title = title,
            Query = query,
            Products = await _catalog.GetProductsAsync(query),
            Filters = await _catalog.GetFiltersAsync(),
        });
    }

    public async Task<IActionResult> Product(int id)
    {
        var product = await _catalog.GetProductAsync(id);
        if (product == null) return NotFound();
        var filters = await _catalog.GetFiltersAsync();
        var mine = product.Filters.GetValueOrDefault("size") ?? new();

        // Показываем линейку размеров того же типа (одежда / обувь / one size)
        var all = filters.First(f => f.Code == "size").Options.Select(o => o.Value).ToList();
        List<string> scale;
        if (mine.Contains("ONE SIZE")) scale = new() { "ONE SIZE" };
        else if (mine.Any(s => char.IsDigit(s[0]))) scale = all.Where(s => char.IsDigit(s[0])).ToList();
        else scale = all.Where(s => !char.IsDigit(s[0]) && s != "ONE SIZE").ToList();

        return View(new ProductViewModel { Product = product, AllSizes = scale });
    }
}

/// <summary>Корзина.</summary>
[Authorize]
public class CartController : Controller
{
    private readonly CartService _cart;

    public CartController(CartService cart) => _cart = cart;

    public async Task<IActionResult> Index() => View(await _cart.GetCartAsync(User.GetUserId()!.Value));
}

/// <summary>Вход, регистрация и профиль.</summary>
public class AccountController : Controller
{
    private readonly AccountService _accounts;

    public AccountController(AccountService accounts) => _accounts = accounts;

    [Authorize]
    public async Task<IActionResult> Index([FromServices] AppDbContext db, [FromServices] CartService cart)
    {
        var id = User.GetUserId()!.Value;
        var user = await db.Users.FindAsync(id);
        if (user == null)
        {
            await AccountService.SignOutAsync(HttpContext);
            return RedirectToAction(nameof(Login));
        }
        return View(new AccountViewModel
        {
            User = new UserDto(user.Id, user.Name, user.Email, user.Phone, user.CreatedAt),
            Cart = await cart.GetCartAsync(id),
        });
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequest());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _accounts.CheckCredentialsAsync(model.Name, model.Email, model.Password);
        if (user == null)
        {
            ModelState.AddModelError("", "Wrong username, email or password");
            return View(model);
        }

        await AccountService.SignInAsync(HttpContext, user);
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Shop");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction(nameof(Index));
        return View(new RegisterRequest());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        var passwordError = AccountService.ValidatePassword(model.Password);
        if (passwordError != null) ModelState.AddModelError(nameof(model.Password), passwordError);
        if (!ModelState.IsValid) return View(model);

        var (user, error) = await _accounts.RegisterAsync(model.Name, model.Email, model.Phone, model.Password);
        if (user == null)
        {
            ModelState.AddModelError(nameof(model.Email), error!);
            return View(model);
        }

        await AccountService.SignInAsync(HttpContext, user);
        return RedirectToAction("Index", "Shop");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await AccountService.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "News");
    }
}
