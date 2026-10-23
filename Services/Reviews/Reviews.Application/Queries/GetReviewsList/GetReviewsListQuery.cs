using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Queries.GetReviewsList;

public record GetReviewsListQuery(long? ProductId, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ReviewDto>>;