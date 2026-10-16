using AutoMapper;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;

namespace Orders.Bll.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ProductDto?> GetByIdAsync(long productId, CancellationToken ct = default)
    {
        var product = await _uow.Products.GetByIdAsync(productId, ct);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await _uow.Products.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}