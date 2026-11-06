using AutoMapper;
using Orders.Bll.Caching;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;

namespace Orders.Bll.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public ProductService(IUnitOfWork uow, IMapper mapper, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
    }

    public Task<ProductDto?> GetByIdAsync(long productId, CancellationToken ct = default)
        => _cache.GetOrCreateAsync<ProductDto>(OrdersCache.Product(productId), async token =>
        {
            var product = await _uow.Products.GetByIdAsync(productId, token);
            return product == null ? null : _mapper.Map<ProductDto>(product);
        }, OrdersCachePolicies.Product, ct);

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await _cache.GetOrCreateAsync<IEnumerable<ProductDto>>(OrdersCache.Products, async token =>
        {
            var products = await _uow.Products.GetAllAsync(token);
            return _mapper.Map<IEnumerable<ProductDto>>(products).ToList();  
        }, OrdersCachePolicies.Product, ct);

        return result!;
    }
}
