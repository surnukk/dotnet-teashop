using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Reviews.Application.Common.Validation;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands.AddComment;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId).MustBeObjectId();
        RuleFor(x => x.AuthorId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(Author.NameMaxLength);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(Comment.TextMaxLength);
    }
}