using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(Review.ProductNameMaxLength);
        RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Author.NameMaxLength);
        RuleFor(x => x.Rating).InclusiveBetween(Rating.Min, Rating.Max);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Review.TextMaxLength);
    }
}