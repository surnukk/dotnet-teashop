using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Commands.AddComment;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reviews.Application.Commands.RemoveComment;

public class RemoveCommentCommandHandler(IReviewRepository reviews, ILogger<RemoveCommentCommandHandler> logger)
    : IRequestHandler<RemoveCommentCommand>
{
    public async Task Handle(RemoveCommentCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.ReviewId), ct)
            ?? throw new NotFoundException($"Review {cmd.ReviewId} was not found");

        review.RemoveComment(ObjectId.Parse(cmd.CommentId));
        await reviews.UpdateAsync(review, ct);

        logger.LogInformation("Comment {CommentId} removed from review {ReviewId}", cmd.CommentId, cmd.ReviewId);
    }
}