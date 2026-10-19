using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return; 

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var sencha = NewProduct(1, "Sencha Premium", 300m, 50);
        var darjeel = NewProduct(2, "Darjeeling First Flush", 550m, 30);
        var puerh = NewProduct(3, "Shu Pu-erh Aged 2018", 600m, 25);
        var daHongPao = NewProduct(4, "Da Hong Pao Oolong", 450m, 40);
        var gaiwan = NewProduct(5, "Ceramic Gaiwan Set", 850m, 15);

        db.Products.AddRange(sencha, darjeel, puerh, daHongPao, gaiwan);
        await db.SaveChangesAsync(ct); 

        db.ProductDetails.AddRange(
            new ProductDetail
            {
                ProductId = sencha.ProductId,
                Descrip = "Delicate steamed green tea with rich umami flavor.",
                OriginCountry = "Japan",
                Region = "Shizuoka",
                TeaForm = "Loose leaf",
                TeaVariety = "Sencha",
                FlavorNotes = "Grass, seaweed, sweet finish",
                WeightGrams = 100
            },
            new ProductDetail
            {
                ProductId = darjeel.ProductId,
                Descrip = "Champagne of teas from the Himalayan foothills.",
                OriginCountry = "India",
                Region = "Darjeeling",
                TeaForm = "Loose leaf",
                TeaVariety = "Black",
                FlavorNotes = "Muscatel, floral, honey",
                WeightGrams = 100
            },
            new ProductDetail
            {
                ProductId = puerh.ProductId,
                Descrip = "Earthy and smooth fermented dark tea cake.",
                OriginCountry = "China",
                Region = "Yunnan",
                TeaForm = "Compressed cake",
                TeaVariety = "Shu Pu-erh",
                FlavorNotes = "Earth, wood, walnut",
                WeightGrams = 150
            },
            new ProductDetail
            {
                ProductId = daHongPao.ProductId,
                Descrip = "Legendary rock oolong with complex mineral profile.",
                OriginCountry = "China",
                Region = "Wuyi Mountains",
                TeaForm = "Rolled leaves",
                TeaVariety = "Oolong",
                FlavorNotes = "Mineral, orchid, roasted nuts",
                WeightGrams = 100
            },
            new ProductDetail
            {
                ProductId = gaiwan.ProductId,
                Descrip = "Traditional Chinese tea brewing bowl and lid.",
                OriginCountry = "China",
                Region = "Fujian",
                TeaForm = "Hardware",
                TeaVariety = "Accessories",
                FlavorNotes = "Neutral ceramic",
                WeightGrams = 250
            });

        db.ProductImages.AddRange(
            new ProductImage { ProductId = sencha.ProductId, IsMain = true, ImageUrl = "https://cdn.oycha.com/public/products-media/sencha-1-800x800.webp" },
            new ProductImage { ProductId = darjeel.ProductId, IsMain = true, ImageUrl = "https://cdn.oycha.com/public/products-media/darjeeling-1st-flush-1-800x800.webp" },
            new ProductImage { ProductId = puerh.ProductId, IsMain = true, ImageUrl = "https://cdn.oycha.com/public/products-media/shu-puerh-1st-grade-1-800x800.webpg" },
            new ProductImage { ProductId = daHongPao.ProductId, IsMain = true, ImageUrl = "https://cdn.oycha.com/public/products-media/da-hong-pao-1-800x800.webp" },
            new ProductImage { ProductId = gaiwan.ProductId, IsMain = true, ImageUrl = "https://cdn.oycha.com/public/products-media/1%20(8)-800x800.webp" });

        db.ProductTags.AddRange(
            new ProductTag { ProductId = sencha.ProductId, TagId = 1 },
            new ProductTag { ProductId = sencha.ProductId, TagId = 2 },
            new ProductTag { ProductId = darjeel.ProductId, TagId = 4 },
            new ProductTag { ProductId = puerh.ProductId, TagId = 2 },
            new ProductTag { ProductId = puerh.ProductId, TagId = 5 },
            new ProductTag { ProductId = daHongPao.ProductId, TagId = 3 },
            new ProductTag { ProductId = daHongPao.ProductId, TagId = 4 },
            new ProductTag { ProductId = gaiwan.ProductId, TagId = 2 });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);                       
    }

    private static Product NewProduct(int categoryId, string name, decimal price, int stock)
    {
        var product = new Product(categoryId, name, price);
        product.AddStock(stock);
        return product;
    }
}