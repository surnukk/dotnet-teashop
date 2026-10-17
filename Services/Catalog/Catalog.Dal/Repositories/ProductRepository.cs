using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductRepository(AppDbContext context)
    : GenericRepository<Product>(context), IProductRepository
{
    // Eager Loading: картка товару з усім пов'язаним одним запитом
    public Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => Set.Include(p => p.Category)
              .Include(p => p.Detail)
              .Include(p => p.Images)
              .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
              .FirstOrDefaultAsync(p => p.ProductId == id, ct);

    public Task<bool> ExistsByNameAsync(string productName, CancellationToken ct = default)
        => Set.AnyAsync(p => p.ProductName == productName, ct);
}