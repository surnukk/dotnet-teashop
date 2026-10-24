using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Reviews.Domain.Entities;
using Reviews.Domain.Interfaces;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator(IReviewRepository reviews)
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.ProductName).NotEmpty().MaximumLength(Review.ProductNameMaxLength);
        RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Author.NameMaxLength);
        RuleFor(x => x.Rating).InclusiveBetween(Rating.Min, Rating.Max);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Review.TextMaxLength);

        // асинхронна перевірка в MongoDB: один відгук від автора на товар
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) => !await reviews.ExistsAsync(cmd.ProductId, cmd.AuthorId, ct))
            .WithMessage("You have already reviewed this product")
            .OverridePropertyName(nameof(CreateReviewCommand.AuthorId))
            .When(x => x.ProductId > 0 && !string.IsNullOrWhiteSpace(x.AuthorId));   // без зайвого запиту в БД
    }
}