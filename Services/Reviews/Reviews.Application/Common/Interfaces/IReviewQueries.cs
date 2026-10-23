using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Common.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct);

    Task<PagedResult<ReviewDto>> GetListAsync(
        long? productId, int page, int pageSize, CancellationToken ct);
}