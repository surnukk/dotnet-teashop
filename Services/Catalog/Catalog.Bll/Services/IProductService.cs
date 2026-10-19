using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Catalog.Bll.DTO;

namespace Catalog.Bll.Services;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetListAsync(ProductQuery query, CancellationToken ct);
    Task<ProductDetailsDto> GetByIdAsync(int id, CancellationToken ct);
    Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct);
    Task<ProductDto> ChangePriceAsync(int id, ChangePriceDto input, CancellationToken ct);
    Task<ProductDto> AddStockAsync(int id, StockChangeDto input, CancellationToken ct);
    Task<ProductDto> RemoveStockAsync(int id, StockChangeDto input, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task AddTagAsync(int productId, int tagId, CancellationToken ct);
    Task RemoveTagAsync(int productId, int tagId, CancellationToken ct);
}