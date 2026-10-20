using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Catalog.Dal.Repositories;
using Catalog.Domain;

namespace Catalog.Dal.Specifications;

public class ProductsFilterSpec : Specification<Product>
{
    public ProductsFilterSpec(ProductFilter f)
    {
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var search = f.Search.Trim();
            Query.Where(p => p.ProductName.Contains(search));
        }
        if (f.CategoryId is not null)
            Query.Where(p => p.CategoryId == f.CategoryId);
        if (f.TagId is not null)
            Query.Where(p => p.ProductTags.Any(pt => pt.TagId == f.TagId));
        if (f.MinPrice is not null)
            Query.Where(p => p.Price >= f.MinPrice);
        if (f.MaxPrice is not null)
            Query.Where(p => p.Price <= f.MaxPrice);
        if (f.IsAvailable is not null)
            Query.Where(p => p.IsAvailable == f.IsAvailable);
    }
}