using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;

namespace Catalog.Dal.Repositories;

public interface IProductTagRepository : IGenericRepository<ProductTag>
{
    Task<IReadOnlyList<Product>> GetRecentlyAddedAsync(
        string tagName, DateTime since, CancellationToken ct = default);

    Task<ProductTag?> GetAsync(int productId, int tagId, CancellationToken ct = default);
}