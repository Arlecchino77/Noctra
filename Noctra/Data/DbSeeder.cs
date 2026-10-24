using Noctra.Models;

namespace Noctra.Data;

/// <summary>Заполняет пустую базу категориями, коллекциями, фильтрами, товарами с фото и новостями.</summary>
public static class DbSeeder
{
    private static readonly string[] ClothesSizes = { "XS", "S", "M", "L", "XL", "XXL" };
    private static readonly string[] ShoeSizes = { "39", "40", "41", "42", "43", "44" };
    private const string OneSize = "ONE SIZE";

    private record ProductSeed(
        string Name, string Category, string Collection, decimal Price, string Image,
        string Color, string Material, string MaterialText, string[] Sizes, string Description, bool IsNew = false);

    public static void Seed(AppDbContext db, string seedDir)
    {
        if (db.Categories.Any()) return;

        var categories = new[]
        {
            ("JACKETS", "jackets"), ("COATS", "coats"), ("SWEATERS", "sweaters"), ("JEANS", "jeans"),
            ("SHOES", "shoes"), ("TOPS", "tops"), ("ACCESSORIES", "accessories"),
        }.Select((c, i) => new Category { Name = c.Item1, Slug = c.Item2, SortOrder = i }).ToList();
        db.Categories.AddRange(categories);

        var collections = new List<Collection>
        {
            new() { Name = "SPRING 2026 COLLECTION", Slug = "spring", CoverPath = "/img/collections/spring.jpg", SortOrder = 0,
                Description = "Rain, leather and long silhouettes. The spring drop is built around heavy coats and high boots." },
            new() { Name = "SUMMER 2026 COLLECTION", Slug = "summer", CoverPath = "/img/collections/summer.jpg", SortOrder = 1,
                Description = "Washed fabrics, wide trousers and oversized layers for hot city nights." },
            new() { Name = "AUTUMN 2026 COLLECTION", Slug = "autumn", CoverPath = "/img/collections/autumn.jpg", SortOrder = 2,
                Description = "Asymmetric cuts, raw denim and dense knitwear for the cold season." },
            new() { Name = "COLLECTION FROM THE SHOW IN TOKYO", Slug = "tokyo", CoverPath = "/img/collections/tokyo.jpg", SortOrder = 3,
                Description = "Looks presented at the Tokyo fashion show: technical outerwear and runway pieces." },
            new() { Name = "JEWELRY COLLECTION", Slug = "jewelry", CoverPath = "/img/collections/jewelry.jpg", SortOrder = 4,
                Description = "Hand-finished 925 silver rings, chains and leather goods." },
        };
        db.Collections.AddRange(collections);

        var filterDefs = new (string Name, string Code, string[] Values)[]
        {
            ("SIZE", "size", ClothesSizes.Concat(ShoeSizes).Append(OneSize).ToArray()),
            ("COLOR", "color", new[] { "BLACK", "GREY", "WHITE", "RED", "SILVER" }),
            ("MATERIAL", "material", new[] { "LEATHER", "COTTON", "DENIM", "WOOL", "NYLON", "SILVER" }),
        };
        var options = new Dictionary<(string, string), FilterOption>();
        for (var i = 0; i < filterDefs.Length; i++)
        {
            var (name, code, values) = filterDefs[i];
            var filter = new Filter { Name = name, Code = code, SortOrder = i };
            for (var j = 0; j < values.Length; j++)
            {
                var option = new FilterOption { Value = values[j], SortOrder = j };
                filter.Options.Add(option);
                options[(code, values[j])] = option;
            }
            db.Filters.Add(filter);
        }

        var products = new[]
        {
            new ProductSeed("RED LEATHER JACKET", "jackets", "spring", 1900, "spring-jacket.jpg", "RED", "LEATHER",
                "Distressed lamb leather, double zip", ClothesSizes,
                "Fitted biker jacket in deep red leather with raw frayed hems.", true),
            new ProductSeed("DARKER THAN BLACK RAINCOAT", "coats", "spring", 3700, "trench.jpg", "BLACK", "LEATHER",
                "Lacquered calf leather, viscose lining, metal buckles", ClothesSizes[1..],
                "Floor-length raincoat with a high collar and a belted waist. The key piece of the spring collection.", true),
            new ProductSeed("LONG LEATHER BOOTS", "shoes", "spring", 1450, "long-leater-boots.jpg", "BLACK", "LEATHER",
                "Calf leather, lacing on the shaft, rubber sole", ShoeSizes,
                "Over-the-knee boots with front lacing and a stacked heel."),
            new ProductSeed("WOMEN'S LEATHER COAT", "coats", "spring", 2900, "spring-coat.jpg", "BLACK", "LEATHER",
                "Soft nappa leather, cape sleeves", ClothesSizes[..5],
                "Cape-cut leather coat with harness straps and a draped back."),
            new ProductSeed("TABI LEATHER BOOTS", "shoes", "spring", 980, "tabi-shoes.jpg", "BLACK", "LEATHER",
                "Polished leather, split toe, block heel", ShoeSizes,
                "Split-toe tabi boots on a chunky platform."),
            new ProductSeed("ASYMMETRIC BUTTON JACKET", "jackets", "autumn", 1250, "autumn-jacket.jpg", "BLACK", "WOOL",
                "Wool blend, side button placket", ClothesSizes,
                "Slim jacket with an asymmetric front and a stand-up collar.", true),
            new ProductSeed("PANELED FLARED JEANS", "jeans", "autumn", 640, "autumn-jeans.jpg", "BLACK", "DENIM",
                "Washed black denim, metal hardware", ClothesSizes,
                "Flared jeans with curved panels and decorative straps."),
            new ProductSeed("CONTOUR KNIT SWEATER", "sweaters", "autumn", 520, "autumn-sweater.jpg", "WHITE", "WOOL",
                "Merino wool, relief knit", ClothesSizes,
                "Oversized sweater with a raised contour pattern on the sleeves."),
            new ProductSeed("CHROME CLAW GLOVES", "accessories", "autumn", 2000, "gloves.jpg", "BLACK", "LEATHER",
                "Leather, silver-plated plates", new[] { "S", "M", "L" },
                "Limited series of 200 pairs made in collaboration with Chrome Hearts."),
            new ProductSeed("OVERSIZED CARGO JACKET", "jackets", "summer", 890, "summer-jacket.jpg", "GREY", "COTTON",
                "Garment-dyed cotton canvas", ClothesSizes,
                "Boxy cargo jacket with huge sleeves and washed finish.", true),
            new ProductSeed("LAYERED BLACK SUIT", "tops", "summer", 1600, "summer-costume.jpg", "BLACK", "COTTON",
                "Light cotton, wide trousers, wrap jacket", ClothesSizes,
                "Two-piece summer suit with a wrap jacket and a long pleated skirt-trousers."),
            new ProductSeed("ROSE ACID-WASH T-SHIRT", "tops", "summer", 210, "summer-tshirt.jpg", "GREY", "COTTON",
                "Heavy cotton jersey, acid wash, puff print", ClothesSizes,
                "Oversized tee with a 3D black rose print."),
            new ProductSeed("3XL SNEAKERS", "shoes", "tokyo", 1100, "balenciaga-3xl.jpg", "BLACK", "NYLON",
                "Mesh and rubber, worn-out effect", ShoeSizes,
                "Chunky runner sneakers with layered sole."),
            new ProductSeed("TRADITIONAL COSTUME", "coats", "tokyo", 2400, "a-fashion-show-in-japan.jpg", "BLACK", "WOOL",
                "Wool, printed silk panel", ClothesSizes[1..],
                "Long coat with a printed silk panel, closing look of the Tokyo show."),
            new ProductSeed("DRIFT COAT", "coats", "tokyo", 2100, "fashion-japan-2.jpg", "BLACK", "COTTON",
                "Cotton gabardine, raw edges", ClothesSizes,
                "Deconstructed trench from the Tokyo show."),
            new ProductSeed("TOKYO HOODED ANORAK", "jackets", "tokyo", 1350, "jacket.jpg", "BLACK", "NYLON",
                "Water-repellent nylon, taped seams", ClothesSizes,
                "Technical anorak shown at the Tokyo fashion show.", true),
            new ProductSeed("TOKYO RING", "accessories", "tokyo", 340, "tokyo-ring.jpg", "SILVER", "SILVER",
                "925 sterling silver, hand-polished", new[] { OneSize },
                "Chunky ring with a spiral relief, made for the Tokyo show."),
            new ProductSeed("LOTUS SIGNET RING", "accessories", "jewelry", 380, "ring.jpg", "SILVER", "SILVER",
                "925 sterling silver, oxidised", new[] { OneSize },
                "Massive signet ring with an engraved lotus."),
            new ProductSeed("THORN SILVER RING", "accessories", "jewelry", 290, "silver-ring.jpg", "SILVER", "SILVER",
                "925 sterling silver", new[] { OneSize },
                "Ring with sharp thorns wrapped around the band."),
            new ProductSeed("CROWN BAND RING", "accessories", "jewelry", 310, "autumn.jpg", "SILVER", "SILVER",
                "925 sterling silver", new[] { OneSize },
                "Wide band ring with a molten crown edge."),
            new ProductSeed("CUBE PENDANT CHAIN", "accessories", "jewelry", 450, "silver-chain.jpg", "SILVER", "SILVER",
                "925 sterling silver, 60 cm chain", new[] { OneSize },
                "Heavy chain with a carved cube pendant."),
            new ProductSeed("CROSS LEATHER WALLET", "accessories", "jewelry", 560, "wallet.jpg", "BLACK", "LEATHER",
                "Bridle leather, silver cross, chain", new[] { OneSize },
                "Zip wallet with a silver cross and a wallet chain."),
        };

        foreach (var s in products)
        {
            var product = new Product
            {
                Name = s.Name,
                Description = s.Description,
                Material = s.MaterialText,
                Price = s.Price,
                IsNew = s.IsNew,
                Category = categories.First(c => c.Slug == s.Category),
                Collection = collections.First(c => c.Slug == s.Collection),
            };
            foreach (var size in s.Sizes) product.FilterOptions.Add(options[("size", size)]);
            product.FilterOptions.Add(options[("color", s.Color)]);
            product.FilterOptions.Add(options[("material", s.Material)]);

            var path = Path.Combine(seedDir, "products", s.Image);
            if (File.Exists(path))
            {
                product.Images.Add(new ProductImage
                {
                    FileName = s.Image,
                    ContentType = s.Image.EndsWith(".png") ? "image/png" : "image/jpeg",
                    Data = File.ReadAllBytes(path),
                });
            }
            db.Products.Add(product);
        }

        db.Articles.AddRange(
            new Article { Title = "COPENHAGEN", Tag = "PHOTO SHOOT", Location = "IN COPENHAGEN", Subtitle = "PHOTO SHOOT IN", ImagePath = "/img/news/copenhagen.jpg",
                PublishedAt = new DateTime(2026, 10, 22),
                Body = "The new lookbook was shot on the rainy streets of Copenhagen: oversized coats, wide trousers and coffee at La Cabra." },
            new Article { Title = "NEW FASHION SHOW", Tag = "FASHION SHOW", Location = "IN MOSCOW", Subtitle = "IN MOSKOW", ImagePath = "/img/news/moscow.png",
                PublishedAt = new DateTime(2026, 11, 1),
                Body = "NØCTRA presented a new show in Moscow, where the main theme was the armour of the future." },
            new Article { Title = "CAME UP WITH A SUIT MADE OF CARBON FIBER", Tag = "NEW DESIGN", Location = "VADIM ZHUKOV", Subtitle = "OUR DESIGNER VADIM ZHUKOV", ImagePath = "/img/news/carbon.jpg",
                PublishedAt = new DateTime(2026, 5, 6),
                Body = "Vadim Zhukov spent a year developing a suit made of carbon fiber. The first piece is a glove that repeats the anatomy of the hand." },
            new Article { Title = "WIN A NOMINATION AT THE TOKYO FASHION SHOW", Tag = "NOMINATION", Location = "IN TOKYO", Subtitle = "TOKYO", ImagePath = "/img/news/tokyo.jpg",
                PublishedAt = new DateTime(2026, 4, 18),
                Body = "The brand received a nomination at the Tokyo fashion show for the collection inspired by Japanese ink painting." },
            new Article { Title = "A NEW COLLECTION HAS BEEN RELEASED", Tag = "RELEASE", Location = "SPRING 2026", Subtitle = "NEW COLLECTION", ImagePath = "/img/news/new-collection.jpg",
                PublishedAt = new DateTime(2026, 3, 30),
                Body = "The spring 2026 collection is already available in the shop: leather coats, high boots and silver jewelry." },
            new Article { Title = "GOSHA RUBCHINSKIY AND HIS NEW COLLECTION IN COLLABORATION WITH US", Tag = "COLLABORATION", Location = "GOSHA RUBCHINSKIY", Subtitle = "COLLABORATION", ImagePath = "/img/news/gosha.jpg",
                PublishedAt = new DateTime(2026, 10, 22),
                Body = "Gosha Rubchinskiy and NØCTRA released a joint knitwear capsule. The collection is now available." }
        );

        db.SaveChanges();
    }
}
