using MediatR;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using Reviews.Application.Commands.DeleteReview;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reviews.Application.Commands.AddComment;

public class AddCommentCommandHandler(IReviewRepository reviews, ILogger<AddCommentCommandHandler> logger)
    : IRequestHandler<AddCommentCommand, string>
{
    public async Task<string> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.ReviewId), ct)
            ?? throw new NotFoundException($"Review {cmd.ReviewId} was not found");

        var comment = review.AddComment(new Author(cmd.AuthorId, cmd.AuthorName), cmd.Text);
        await reviews.UpdateAsync(review, ct);

        logger.LogInformation("Comment {CommentId} added to review {ReviewId}", comment.Id, cmd.ReviewId);

        return comment.Id.ToString();
    }
}