using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain;

public class Product
{
    public int ProductId { get; private set; }
    public int CategoryId { get; private set; }
    public string ProductName { get; private set; } = "";
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public bool IsAvailable { get; private set; }

    public Category Category { get; private set; } = null!; // N:1
    public ProductDetail? Detail { get; private set; } // 1:1
    public List<ProductImage> Images { get; } = new(); // 1:N
    public List<ProductTag> ProductTags { get; } = new(); // M:N

    private Product() { }        

    public Product(int categoryId, string productName, decimal price)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name must not be empty");
        if (price <= 0)
            throw new DomainException("Price must be positive");

        CategoryId = categoryId;
        ProductName = productName.Trim();
        Price = price;
        IsAvailable = true;
    }

    public void ChangePrice(decimal price)
    {
        if (price <= 0) throw new DomainException("Price must be positive");
        Price = price;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
        if (StockQuantity < quantity)
            throw new DomainException($"Only {StockQuantity} units available");
        StockQuantity -= quantity;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");
        StockQuantity += quantity;
    }
}
