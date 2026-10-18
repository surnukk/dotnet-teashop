using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Domain;

public class Category
{
    public int CategoryId { get; private set; }
    public string CategoryName { get; private set; } = "";
    public string? Descrip { get; private set; }

    public List<Product> Products { get; } = new(); 

    private Category() { }

    public Category(string categoryName, string? descrip = null)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new DomainException("Category name must not be empty");
        CategoryName = categoryName.Trim();
        Descrip = descrip;
    }

    public void Update(string categoryName, string? descrip)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new DomainException("Category name must not be empty");
        CategoryName = categoryName.Trim();
        Descrip = descrip;
    }
}