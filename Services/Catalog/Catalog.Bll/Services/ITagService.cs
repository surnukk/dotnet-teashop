using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Bll.DTO;

namespace Catalog.Bll.Services;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct);
    Task<TagDto> GetByIdAsync(int id, CancellationToken ct);
    Task<TagDto> CreateAsync(CreateTagDto input, CancellationToken ct);
    Task<IReadOnlyList<ProductDto>> GetRecentlyAddedProductsAsync(
        string tagName, DateTime since, CancellationToken ct);
}