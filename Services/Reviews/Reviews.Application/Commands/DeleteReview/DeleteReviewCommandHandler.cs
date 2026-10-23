using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Commands.DeleteReview;

public class DeleteReviewCommandHandler(IReviewRepository reviews)
    : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand cmd, CancellationToken ct)
    {
        var deleted = await reviews.DeleteAsync(ObjectId.Parse(cmd.Id), ct);
        if (!deleted)
            throw new NotFoundException($"Review {cmd.Id} was not found");
    }
}