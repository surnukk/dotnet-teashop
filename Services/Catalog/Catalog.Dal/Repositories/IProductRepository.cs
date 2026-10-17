using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;

namespace Catalog.Dal.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<Product?> GetWithDetailsAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string productName, CancellationToken ct = default);
}
