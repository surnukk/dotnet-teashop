using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Domain;

namespace Catalog.Dal.Repositories;

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<bool> ExistsByNameAsync(string tagName, CancellationToken ct = default);
}