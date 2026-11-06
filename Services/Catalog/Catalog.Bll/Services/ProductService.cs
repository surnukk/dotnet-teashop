using AutoMapper;
using Catalog.Bll.Caching;
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
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Catalog.Bll.Services;

public class ProductService(IUnitOfWork uow, IMapper mapper, ILogger<ProductService> logger, ICacheService cache) : IProductService
{
    private static readonly ActivitySource Source = new("Catalog.Api");

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

        var key = CatalogCache.ProductList(page, pageSize, q.Search, q.CategoryId, q.TagId,
                                           q.MinPrice, q.MaxPrice, q.IsAvailable, sortBy, descending);

        var result = await cache.GetOrCreateAsync<PagedResult<ProductDto>>(key, async token =>
        {
            var items = await uow.Products.ListAsync(new ProductsPageSpec(filter), token);
            var total = await uow.Products.CountAsync(new ProductsFilterSpec(filter), token);
            return new PagedResult<ProductDto>(mapper.Map<List<ProductDto>>(items), total, page, pageSize);
        }, CatalogCachePolicies.List, ct);

        return result!;
    }

    public async Task<ProductDetailsDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var dto = await cache.GetOrCreateAsync<ProductDetailsDto>(CatalogCache.Product(id), async token =>
        {
            var product = await uow.Products.GetWithDetailsAsync(id, token);
            return product is null ? null : mapper.Map<ProductDetailsDto>(product);
        }, CatalogCachePolicies.ProductCard, ct);

        return dto ?? throw new EntityNotFoundException(nameof(Product), id);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto input, CancellationToken ct)
    {
        if (await uow.Categories.GetByIdAsync(input.CategoryId, ct) is null)
            throw new EntityNotFoundException(nameof(Category), input.CategoryId);

        if (await uow.Products.ExistsByNameAsync(input.ProductName.Trim(), ct))
            throw new ConflictException($"Product '{input.ProductName}' already exists");

        var product = new Product(input.CategoryId, input.ProductName, input.Price);
        await uow.Products.AddAsync(product, ct);
        await uow.SaveChangesAsync(ct);

        await cache.RemoveGroupAsync(CatalogCache.ListsGroup, "ProductCreated");

        logger.LogInformation("Product {ProductId} created in category {CategoryId}, price {Price}",
            product.ProductId, product.CategoryId, product.Price);

        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> ChangePriceAsync(int id, ChangePriceDto input, CancellationToken ct)
    {
        using var activity = Source.StartActivity("ChangeProductPrice");
        activity?.SetTag("product.id", id);

        var product = await GetOrThrowAsync(id, ct);
        var oldPrice = product.Price;
        product.ChangePrice(input.Price);
        await uow.SaveChangesAsync(ct);
        await InvalidateProductAsync(id, "ProductPriceChanged");   // синхронно, одразу після запису

        logger.LogInformation("Product {ProductId} price changed from {OldPrice} to {NewPrice}",
            id, oldPrice, product.Price);

        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> AddStockAsync(int id, StockChangeDto input, CancellationToken ct)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.AddStock(input.Quantity);              // кількість > 0: перевіряє сутність
        await uow.SaveChangesAsync(ct);
        await InvalidateProductAsync(id, "ProductStockAdded");

        logger.LogInformation("Product {ProductId} stock increased by {Quantity}, now {Stock}",
            id, input.Quantity, product.StockQuantity);

        return mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> RemoveStockAsync(int id, StockChangeDto input, CancellationToken ct)
    {
        var product = await GetOrThrowAsync(id, ct);
        product.RemoveStock(input.Quantity);           // достатній залишок: перевіряє сутність
        await uow.SaveChangesAsync(ct);
        await InvalidateProductAsync(id, "ProductStockRemoved");

        logger.LogInformation("Product {ProductId} stock decreased by {Quantity}, now {Stock}",
            id, input.Quantity, product.StockQuantity);

        return mapper.Map<ProductDto>(product);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var product = await GetOrThrowAsync(id, ct);
        uow.Products.Delete(product);
        await uow.SaveChangesAsync(ct);
        await InvalidateProductAsync(id, "ProductDeleted");
        logger.LogInformation("Product {ProductId} deleted", id);
    }

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
        await InvalidateProductAsync(productId, "ProductTagAdded");   // теги є в картці й у фільтрі списків
        logger.LogInformation("Tag {TagId} added to product {ProductId}", tagId, productId);
    }

    public async Task RemoveTagAsync(int productId, int tagId, CancellationToken ct)
    {
        var link = await uow.ProductTags.GetAsync(productId, tagId, ct)
            ?? throw new EntityNotFoundException(nameof(ProductTag), $"{productId}/{tagId}");

        uow.ProductTags.Delete(link);
        await uow.SaveChangesAsync(ct);
        await InvalidateProductAsync(productId, "ProductTagRemoved");
        logger.LogInformation("Tag {TagId} removed from product {ProductId}", tagId, productId);
    }

	private async Task InvalidateProductAsync(int id, string reason)
	{
		await cache.RemoveAsync(CatalogCache.Product(id), reason);
		await cache.RemoveGroupAsync(CatalogCache.ListsGroup, reason);
	}
}
