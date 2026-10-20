using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ardalis.Specification.EntityFrameworkCore;
using Ardalis.Specification;
using Catalog.Dal.Specifications;
using Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class ProductRepository(AppDbContext context)
    : GenericRepository<Product>(context), IProductRepository
{
    public Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => Set.WithSpecification(new ProductWithDetailsSpec(id))
              .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsByNameAsync(string productName, CancellationToken ct = default)
        => Set.AnyAsync(p => p.ProductName == productName, ct);

    public Task<List<Product>> ListAsync(ISpecification<Product> spec, CancellationToken ct = default)
        => Set.WithSpecification(spec).ToListAsync(ct);

    public Task<int> CountAsync(ISpecification<Product> spec, CancellationToken ct = default)
        => Set.WithSpecification(spec).CountAsync(ct);
}