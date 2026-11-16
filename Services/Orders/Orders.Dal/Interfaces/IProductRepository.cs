using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<bool> CreateSnapshotAsync(long productId, string name, decimal price, DateTime at, CancellationToken ct = default);
    Task<bool> UpdatePriceSnapshotAsync(long productId, string name, decimal price, DateTime at, CancellationToken ct = default);
    Task<bool> UpdateNameSnapshotAsync(long productId, string name, DateTime at, CancellationToken ct = default);
    Task<bool> DeleteSnapshotAsync(long productId, CancellationToken ct = default);
}