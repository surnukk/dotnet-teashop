using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;
using Ardalis.Specification;

namespace Catalog.Dal.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string productName, CancellationToken ct = default);
    Task<List<Product>> ListAsync(ISpecification<Product> spec, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<Product> spec, CancellationToken ct = default);
}
