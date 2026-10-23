using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;

namespace Reviews.Application.Queries.GetReviewById;

public record GetReviewByIdQuery(string Id) : IQuery<ReviewDto>;