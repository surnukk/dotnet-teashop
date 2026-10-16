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

public interface IPromotionService
{
    Task<PromotionDto?> GetByIdAsync(long promotionId, CancellationToken ct = default);
    Task<PromotionDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync(CancellationToken ct = default);
    Task<long> CreateAsync(UpsertPromotionRequest request, CancellationToken ct = default);
    Task UpdateAsync(long promotionId, UpsertPromotionRequest request, CancellationToken ct = default);
    Task DeleteAsync(long promotionId, CancellationToken ct = default);
}
