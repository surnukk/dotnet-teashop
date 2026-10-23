using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;

namespace Reviews.Application.Commands.RemoveComment;

public class RemoveCommentCommandHandler(IReviewRepository reviews)
    : IRequestHandler<RemoveCommentCommand>
{
    public async Task Handle(RemoveCommentCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.ReviewId), ct)
            ?? throw new NotFoundException($"Review {cmd.ReviewId} was not found");

        review.RemoveComment(ObjectId.Parse(cmd.CommentId));
        await reviews.UpdateAsync(review, ct);
    }
}