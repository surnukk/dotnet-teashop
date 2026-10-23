using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Queries.GetReviewsList;

public class GetReviewsListQueryHandler(IReviewQueries queries)
    : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    public Task<PagedResult<ReviewDto>> Handle(GetReviewsListQuery q, CancellationToken ct)
        => queries.GetListAsync(
            q.ProductId,
            Math.Max(q.Page, 1),
            Math.Clamp(q.PageSize, 1, 100),
            ct);
}