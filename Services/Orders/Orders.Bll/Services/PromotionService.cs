using AutoMapper;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;

namespace Orders.Bll.Services;

public class PromotionService : IPromotionService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public PromotionService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PromotionDto?> GetByIdAsync(long promotionId, CancellationToken ct = default)
    {
        var promo = await _uow.Promotions.GetByIdAsync(promotionId, ct);
        return promo == null ? null : _mapper.Map<PromotionDto>(promo);
    }

    public async Task<PromotionDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var promo = await _uow.Promotions.GetByCodeAsync(code, ct);
        return promo == null ? null : _mapper.Map<PromotionDto>(promo);
    }

    public async Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync(CancellationToken ct = default)
    {
        var promos = await _uow.Promotions.GetActivePromotionsAsync(ct);
        return _mapper.Map<IEnumerable<PromotionDto>>(promos);
    }
}