using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Domain.Common;

namespace Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public const int NameMaxLength = 100;

    public string UserId { get; }
    public string DisplayName { get; }

    public Author(string userId, string displayName)
    {
        UserId = Guard.RequiredText(userId, nameof(UserId), 50);
        DisplayName = Guard.RequiredText(displayName, nameof(DisplayName), NameMaxLength);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return DisplayName;
    }
}