using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

/// <summary>Коментар — вкладений документ у масиві comments відгуку.</summary>
public sealed class Comment
{
    public const int TextMaxLength = 1000;

    public ObjectId Id { get; private set; } = ObjectId.GenerateNewId();
    public Author Author { get; private set; } = null!;
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Comment() { }   // для десеріалізації

    public Comment(Author author, string text)
    {
        Author = author ?? throw new Exceptions.DomainException("Comment author is required");
        Text = Guard.RequiredText(text, nameof(Text), TextMaxLength);
    }
}