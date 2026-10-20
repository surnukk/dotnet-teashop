using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Catalog.Dal.Repositories;
using Catalog.Domain;

namespace Catalog.Dal.Specifications;

public class ProductsPageSpec : ProductsFilterSpec
{
    public ProductsPageSpec(ProductFilter f) : base(f)
    {
        switch (f.SortBy, f.Descending)
        {
            case ("price", true):
                Query.OrderByDescending(p => p.Price).ThenBy(p => p.ProductId); break;
            case ("price", false):
                Query.OrderBy(p => p.Price).ThenBy(p => p.ProductId); break;
            case ("stock", true):
                Query.OrderByDescending(p => p.StockQuantity).ThenBy(p => p.ProductId); break;
            case ("stock", false):
                Query.OrderBy(p => p.StockQuantity).ThenBy(p => p.ProductId); break;
            case (_, true):
                Query.OrderByDescending(p => p.ProductName).ThenBy(p => p.ProductId); break;
            default:
                Query.OrderBy(p => p.ProductName).ThenBy(p => p.ProductId); break;
        }

        Query.Skip((f.Page - 1) * f.PageSize)
             .Take(f.PageSize)
             .AsNoTracking();
    }
}