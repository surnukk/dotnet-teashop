using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default);

    Task<bool> ExistsAsync(long productId, string authorId, CancellationToken ct = default);

    Task AddAsync(Review review, CancellationToken ct = default);

    /// <summary>Оновлення з перевіркою Version; при конфлікті → ConflictException.</summary>
    Task UpdateAsync(Review review, CancellationToken ct = default);

    /// <summary>true, якщо документ було видалено.</summary>
    Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default);
}