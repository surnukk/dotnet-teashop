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
}
