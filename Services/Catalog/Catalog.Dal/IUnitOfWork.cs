using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Dal.Repositories;

namespace Catalog.Dal;

public interface IUnitOfWork
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    IProductTagRepository ProductTags { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}