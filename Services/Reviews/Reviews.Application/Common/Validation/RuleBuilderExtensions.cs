using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MongoDB.Bson;

namespace Reviews.Application.Common.Validation;

public static class RuleBuilderExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeObjectId<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(value => !string.IsNullOrWhiteSpace(value) && ObjectId.TryParse(value, out _))
               .WithMessage("'{PropertyName}' must be a valid ObjectId");
}