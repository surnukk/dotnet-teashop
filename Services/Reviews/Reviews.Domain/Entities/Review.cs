using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

/// <summary>Відгук про товар (документ колекції reviews).</summary>
public class Review : BaseEntity
{
    public const int ProductNameMaxLength = 200;
    public const int TextMaxLength = 2000;

    private List<Comment> _comments = new();

    public long ProductId { get; private set; }                  // посилання на Catalog
    public string ProductName { get; private set; } = string.Empty;   // денормалізовано
    public Author Author { get; private set; } = null!;
    public Rating Rating { get; private set; } = null!;
    public string Text { get; private set; } = string.Empty;
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private Review() { }   // для десеріалізації

    public Review(long productId, string productName, Author author, Rating rating, string text)
    {
        if (productId <= 0)
            throw new DomainException("ProductId must be greater than 0");

        ProductId = productId;
        ProductName = Guard.RequiredText(productName, nameof(ProductName), ProductNameMaxLength);
        Author = author ?? throw new DomainException("Author is required");
        Rating = rating ?? throw new DomainException("Rating is required");
        Text = Guard.RequiredText(text, nameof(Text), TextMaxLength);
    }

    public void Update(Rating rating, string text)
    {
        ArgumentNullExceptionIfNull(rating);
        var newText = Guard.RequiredText(text, nameof(Text), TextMaxLength);

        Rating = rating;
        Text = newText;
        Touch();
    }

    public Comment AddComment(Author author, string text)
    {
        var comment = new Comment(author, text);
        _comments.Add(comment);
        Touch();
        return comment;
    }

    public void RemoveComment(ObjectId commentId)
    {
        var comment = _comments.FirstOrDefault(c => c.Id == commentId)
            ?? throw new NotFoundException($"Comment {commentId} was not found");
        _comments.Remove(comment);
        Touch();
    }

    private static void ArgumentNullExceptionIfNull(Rating? rating)
    {
        if (rating is null) throw new DomainException("Rating is required");
    }
}