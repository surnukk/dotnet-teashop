using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification;
using Catalog.Domain;

namespace Catalog.Dal.Specifications;

public class ProductWithDetailsSpec : Specification<Product>
{
    public ProductWithDetailsSpec(int productId)
    {
        Query.Where(p => p.ProductId == productId)
             .Include(p => p.Category)
             .Include(p => p.Detail)
             .Include(p => p.Images)
             .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag);
    }
}