using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Reviews.Application.Common.Validation;

namespace Reviews.Application.Commands.RemoveComment;

public class RemoveCommentCommandValidator : AbstractValidator<RemoveCommentCommand>
{
    public RemoveCommentCommandValidator()
    {
        RuleFor(x => x.ReviewId).MustBeObjectId();
        RuleFor(x => x.CommentId).MustBeObjectId();
    }
}