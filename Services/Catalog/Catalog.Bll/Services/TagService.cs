using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Catalog.Bll.DTO;
using Catalog.Dal;
using Catalog.Domain;

namespace Catalog.Bll.Services;

public class TagService(IUnitOfWork uow, IMapper mapper) : ITagService
{
    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken ct)
    {
        var tags = await uow.Tags.GetAllAsync(ct);
        return mapper.Map<List<TagDto>>(tags);
    }

    public async Task<TagDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var tag = await uow.Tags.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Tag), id);
        return mapper.Map<TagDto>(tag);
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
        return mapper.Map<TagDto>(tag);
    }

    // M:N через ProductTag
    public async Task<IReadOnlyList<ProductDto>> GetRecentlyAddedProductsAsync(
        string tagName, DateTime since, CancellationToken ct)
    {
        var products = await uow.ProductTags.GetRecentlyAddedAsync(tagName, since, ct);
        return mapper.Map<List<ProductDto>>(products);
    }
}