using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;

namespace Catalog.Dal.Repositories;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetWithProductsAsync(int id, CancellationToken ct = default);
}