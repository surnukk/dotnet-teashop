using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Reviews.Application.Queries.GetReviewsList;

public class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0).When(x => x.ProductId.HasValue);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}