using AutoMapper;
using Catalog.Bll.DTO;
using Catalog.Dal;
using Catalog.Dal.Repositories;
using Catalog.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catalog.Dal.Specifications;

namespace Catalog.Bll.Services;

public class ProductService(IUnitOfWork uow, IMapper mapper) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetListAsync(ProductQuery q, CancellationToken ct)
    {
        var page = Math.Max(q.Page, 1);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        var sortBy = q.SortBy?.ToLowerInvariant() switch
        {
            "price" => "price",
            "stock" => "stock",
            _ => "name"
        };
        var descending = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        var filter = new ProductFilter(page, pageSize, q.Search, q.CategoryId, q.TagId,
                                       q.MinPrice, q.MaxPrice, q.IsAvailable, sortBy, descending);

        var items = await uow.Products.ListAsync(new ProductsPageSpec(filter), ct);
        var total = await uow.Products.CountAsync(new ProductsFilterSpec(filter), ct);

        return new PagedResult<ProductDto>(mapper.Map<List<ProductDto>>(items), total, page, pageSize);
    }

    // Eager Loading: картка товару одним запитом
    public async Task<ProductDetailsDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var product = await uow.Products.GetWithDetailsAsync(id, ct)
            ?? throw new EntityNotFoundException(nameof(Product), id);
        return mapper.Map<ProductDetailsDto>(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct)
    {
        // правила, яким потрібні дані з БД, перевіряє сервіс
        if (await uow.Categories.GetByIdAsync(input.CategoryId, ct) is null)
            throw new EntityNotFoundException(nameof(Category), input.CategoryId);

        if (await uow.Products.ExistsByNameAsync(input.ProductName.Trim(), ct))
            throw new ConflictException($"Product '{input.ProductName}' already exists");

        // правила самої сутності (назва не порожня, ціна > 0) перевіряє конструктор
        var product = new Product(input.CategoryId, input.ProductName, input.Price);
        await uow.Products.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> ChangePriceAsync(int id, ChangePriceDto input, CancellationToken ct)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.ChangePrice(input.Price);              // ціна > 0: перевіряє сутність
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> AddStockAsync(int id, StockChangeDto input, CancellationToken ct)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.AddStock(input.Quantity);              // кількість > 0: перевіряє сутність
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> RemoveStockAsync(int id, StockChangeDto input, CancellationToken ct)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.RemoveStock(input.Quantity);           // достатній залишок: перевіряє сутність
        await uow.SaveChangesAsync(ct);
        return mapper.Map<ProductDto>(product);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var product = await GetOrThrowAsync(id, ct);
        uow.Products.Delete(product);
        await uow.SaveChangesAsync(ct);
    }

    // GetByIdAsync повертає відстежувану сутність, тому Update перед збереженням не потрібен
    private async Task<Product> GetOrThrowAsync(int id, CancellationToken ct)
        => await uow.Products.GetByIdAsync(id, ct)
           ?? throw new EntityNotFoundException(nameof(Product), id);

    public async Task AddTagAsync(int productId, int tagId, CancellationToken ct)
    {
        await GetOrThrowAsync(productId, ct);

        if (await uow.Tags.GetByIdAsync(tagId, ct) is null)
            throw new EntityNotFoundException(nameof(Tag), tagId);

        if (await uow.ProductTags.GetAsync(productId, tagId, ct) is not null)
            throw new ConflictException("This tag is already added to the product");

        await uow.ProductTags.AddAsync(new ProductTag { ProductId = productId, TagId = tagId }, ct);
        await uow.SaveChangesAsync(ct);
    }

    public async Task RemoveTagAsync(int productId, int tagId, CancellationToken ct)
    {
        var link = await uow.ProductTags.GetAsync(productId, tagId, ct)
            ?? throw new EntityNotFoundException(nameof(ProductTag), $"{productId}/{tagId}");

        uow.ProductTags.Delete(link);
        await uow.SaveChangesAsync(ct);
    }
}