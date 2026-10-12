using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orders.Bll.DTO;

namespace Orders.Bll.Services;

public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(long productId, CancellationToken ct = default);
    Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken ct = default);
    Task<bool> CheckStockAsync(long productId, int requiredUnits, CancellationToken ct = default);
}
