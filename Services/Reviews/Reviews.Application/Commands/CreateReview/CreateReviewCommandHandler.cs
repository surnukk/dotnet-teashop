using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.CreateReview;

public class CreateReviewCommandHandler(IReviewRepository reviews)
    : IRequestHandler<CreateReviewCommand, string>
{
    public async Task<string> Handle(CreateReviewCommand cmd, CancellationToken ct)
    {
        var review = new Review(
            cmd.ProductId,
            cmd.ProductName,
            new Author(cmd.AuthorId, cmd.AuthorName),
            new Rating(cmd.Rating),
            cmd.Text);

        await reviews.AddAsync(review, ct);
        return review.Id.ToString();
    }
}