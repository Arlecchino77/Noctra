# NØCTRA — интернет-магазин дизайнерской одежды

Сайт на **C# / ASP.NET Core 8 MVC** с **REST API** и базой данных **SQLite**.
Вёрстка сделана по макету Figma (кадры 1280×720), шрифт — Bebas Neue (`wwwroot/fonts`).

## Как запустить

1. Установить [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (или Visual Studio с нагрузкой «ASP.NET и веб-разработка»).
2. Открыть `Noctra.sln` в Visual Studio и нажать **F5**
   или в терминале из этой папки:

   ```
   dotnet run --project Noctra
   ```

3. Открыть http://localhost:5080 — сайт, http://localhost:5080/swagger — все методы API.

При первом запуске файл базы `Noctra/noctra.db` создаётся автоматически и заполняется
категориями, коллекциями, фильтрами, товарами (вместе с фото) и новостями.
Чтобы пересоздать базу с нуля — остановить сайт и удалить `noctra.db`.

## Страницы

| Адрес | Кадр в Figma | Что на странице |
|---|---|---|
| `/` , `/News?id=2` | newsfeed1–5 | Лента новостей: крупное фото выбранной статьи и 5 карточек |
| `/News/Details/{id}` | — | Полный текст статьи |
| `/Shop` | selection | Подборка коллекций |
| `/Shop/Collection/{spring,summer,autumn,tokyo,jewelry}` | spring / summer / autumn / Tokyo / jewelry collection | Товары коллекции |
| `/Shop/Catalog?category=jackets` | — | Категории из меню, поиск и фильтры (размер, цвет, материал, цена, сортировка) |
| `/Shop/Product/{id}` | product page | Товар: описание, материал, таблица размеров, цена, «Add to cart» |
| `/Cart` | cart | Корзина: при наведении на позицию слева показывается товар |
| `/Account/Login`, `/Account/Register` | login, registration | Вход и регистрация |
| `/Account` | — | Профиль и выход |

## База данных (SQLite)

Диаграмма — `database.drawio` (открывается в draw.io / diagrams.net).

| Таблица | Что хранит |
|---|---|
| `Users` | аккаунты: имя, почта (уникальная), телефон, хэш пароля PBKDF2-SHA256 |
| `CartItems` | корзина: пользователь, товар, размер, количество |
| `Categories` | категории: jackets, coats, sweaters, jeans, shoes, tops, accessories |
| `Collections` | коллекции: spring, summer, autumn, tokyo, jewelry |
| `Products` | товары: название, описание, материал, цена, категория, коллекция |
| `ProductImages` | **фото товаров в виде BLOB**, отдаются через `GET /api/images/{id}` |
| `Filters` | фильтры каталога: SIZE, COLOR, MATERIAL |
| `FilterOptions` | значения фильтров: XS…XXL, 39…44, ONE SIZE, BLACK, LEATHER… |
| `ProductFilterOptions` | какие значения фильтров есть у товара (многие ко многим) |
| `Articles` | статьи новостной ленты |

## API

| Метод | Адрес | Описание |
|---|---|---|
| POST | `/api/auth/register` | регистрация (name, email, phone, password, confirmPassword) |
| POST | `/api/auth/login` | вход (name, email, password) |
| POST | `/api/auth/logout` | выход |
| GET | `/api/auth/me` | текущий пользователь |
| GET | `/api/categories` | категории |
| GET | `/api/collections`, `/api/collections/{slug}` | коллекции |
| GET | `/api/filters` | фильтры и их значения |
| GET | `/api/products?search=&category=&collection=&size=&color=&material=&minPrice=&maxPrice=&sort=` | товары с фильтрами |
| GET | `/api/products/{id}` | товар |
| GET | `/api/images/{id}` | фото товара из базы |
| GET | `/api/cart` | корзина |
| POST | `/api/cart` | добавить (productId, size, quantity) |
| PUT | `/api/cart/{id}` | изменить количество |
| DELETE | `/api/cart/{id}`, `/api/cart` | удалить позицию / очистить |
| POST | `/api/cart/order` | оформить заказ |
| GET | `/api/news`, `/api/news/{id}` | новости |

Авторизация — cookie. Методы корзины без входа возвращают `401`.
Пароль: от 8 символов, буквы и цифры.

## Структура

```
Noctra.sln
database.drawio            — диаграмма БД
Noctra/
  Program.cs               — настройка сервисов, БД, авторизации, Swagger
  Models/                  — сущности БД, DTO для API, модели представлений
  Data/                    — AppDbContext (EF Core + SQLite), DbSeeder (начальные данные)
  Services/                — AccountService, CatalogService, CartService
  Controllers/             — MVC-страницы
  Controllers/Api/         — REST API
  Views/                   — Razor-представления по макету
  wwwroot/                 — CSS, JS, шрифт, логотип, иконки, обложки, фото новостей
  SeedData/products/       — фото товаров, которые при первом запуске загружаются в БД
```
