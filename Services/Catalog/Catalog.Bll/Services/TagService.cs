using AutoMapper;
using Catalog.Bll.Caching;
using Catalog.Bll.DTO;
using Catalog.Dal;
using Catalog.Domain;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Bll.Services;

public class TagService(IUnitOfWork uow, IMapper mapper, ILogger<TagService> logger, ICacheService cache) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct)
    {
        var result = await cache.GetOrCreateAsync<IReadOnlyList<TagDto>>(CatalogCache.Tags, async token =>
        {
            var tags = await uow.Tags.GetAllAsync(token);
            return mapper.Map<List<TagDto>>(tags);
        }, CatalogCachePolicies.Reference, ct);

        return result!;
    }

    public async Task<TagDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var dto = await cache.GetOrCreateAsync<TagDto>(CatalogCache.Tag(id), async token =>
        {
            var tag = await uow.Tags.GetByIdAsync(id, token);
            return tag is null ? null : mapper.Map<TagDto>(tag);
        }, CatalogCachePolicies.Reference, ct);

        return dto ?? throw new EntityNotFoundException(nameof(Tag), id);
    }

    public async Task<TagDto> CreateAsync(CreateTagDto input, CancellationToken ct)
    {
        var name = input.TagName?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tag name must not be empty");

        if (await uow.Tags.ExistsByNameAsync(name, ct))
            throw new ConflictException($"Tag '{name}' already exists");

        var tag = new Tag { TagName = name };
        await uow.Tags.AddAsync(tag, ct);
        await uow.SaveChangesAsync(ct);

        await cache.RemoveAsync(CatalogCache.Tags, "TagCreated");

        logger.LogInformation("Tag {TagId} created", tag.TagId);

        return mapper.Map<TagDto>(tag);
    }

    // M:N через ProductTag.
    // НЕ кешуємо: параметр since за замовчуванням = «зараз − 30 днів», тобто щоразу інший ключ → кеш ніколи б не влучав.
    public async Task<IReadOnlyList<ProductDto>> GetRecentlyAddedProductsAsync(
        string tagName, DateTime since, CancellationToken ct)
    {
        var products = await uow.ProductTags.GetRecentlyAddedAsync(tagName, since, ct);
        return mapper.Map<List<ProductDto>>(products);
    }
}
