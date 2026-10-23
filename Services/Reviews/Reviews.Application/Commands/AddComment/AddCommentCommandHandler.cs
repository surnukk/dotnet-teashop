using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.AddComment;

public class AddCommentCommandHandler(IReviewRepository reviews)
    : IRequestHandler<AddCommentCommand, string>
{
    public async Task<string> Handle(AddCommentCommand cmd, CancellationToken ct)
    {
        var review = await reviews.GetByIdAsync(ObjectId.Parse(cmd.ReviewId), ct)
            ?? throw new NotFoundException($"Review {cmd.ReviewId} was not found");

        var comment = review.AddComment(new Author(cmd.AuthorId, cmd.AuthorName), cmd.Text);
        await reviews.UpdateAsync(review, ct);

        return comment.Id.ToString();
    }
}