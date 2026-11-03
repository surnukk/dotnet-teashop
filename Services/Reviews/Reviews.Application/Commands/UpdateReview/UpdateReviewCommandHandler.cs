using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Commands.UpdateReview;

public class UpdateReviewCommandHandler(IReviewRepository reviews, ILogger<UpdateReviewCommandHandler> logger)
    : IRequestHandler<UpdateReviewCommand>
{
    public async Task Handle(UpdateReviewCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.Id), ct)
            ?? throw new NotFoundException($"Review {cmd.Id} was not found");

        if (review.Version != cmd.ExpectedVersion)
            throw new ConflictException(
                $"Review {cmd.Id} was modified (expected version {cmd.ExpectedVersion}, current {review.Version})");

        review.Update(new Rating(cmd.Rating), cmd.Text);
        await reviews.UpdateAsync(review, ct);

        logger.LogInformation("Review {ReviewId} updated, rating {Rating}", cmd.Id, cmd.Rating);
    }
}