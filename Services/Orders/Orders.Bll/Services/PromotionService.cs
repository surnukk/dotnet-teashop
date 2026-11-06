using AutoMapper;
using Orders.Bll.Caching;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public class PromotionService : IPromotionService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;

    public PromotionService(IUnitOfWork uow, IMapper mapper, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _cache = cache;
    }

    public Task<PromotionDto?> GetByIdAsync(long promotionId, CancellationToken ct = default)
        => _cache.GetOrCreateAsync<PromotionDto>(OrdersCache.Promotion(promotionId), async token =>
        {
            var promo = await _uow.Promotions.GetByIdAsync(promotionId, token);
            return promo == null ? null : _mapper.Map<PromotionDto>(promo);
        }, OrdersCachePolicies.Promotion, ct);

    public Task<PromotionDto?> GetByCodeAsync(string code, CancellationToken ct = default)
        => _cache.GetOrCreateAsync<PromotionDto>(OrdersCache.PromotionByCode(code), async token =>
        {
            var promo = await _uow.Promotions.GetByCodeAsync(code, token);
            return promo == null ? null : _mapper.Map<PromotionDto>(promo);
        }, OrdersCachePolicies.Promotion, ct);

    public async Task<IEnumerable<PromotionDto>> GetActivePromotionsAsync(CancellationToken ct = default)
    {
        var result = await _cache.GetOrCreateAsync<IEnumerable<PromotionDto>>(OrdersCache.ActivePromotions, async token =>
        {
            var promos = await _uow.Promotions.GetActivePromotionsAsync(token);
            return _mapper.Map<IEnumerable<PromotionDto>>(promos).ToList();
        }, OrdersCachePolicies.Promotion, ct);

        return result!;
    }

    public async Task<long> CreateAsync(UpsertPromotionRequest request, CancellationToken ct = default)
    {
        ValidateDates(request);
        var promo = _mapper.Map<Promotion>(request);
        var id = await _uow.Promotions.AddAsync(promo, ct);

        _cache.RemoveGroup(OrdersCache.PromotionsGroup, "PromotionCreated");  
        return id;
    }

    public async Task UpdateAsync(long promotionId, UpsertPromotionRequest request, CancellationToken ct = default)
    {
        ValidateDates(request);

        var promo = await _uow.Promotions.GetByIdAsync(promotionId, ct)
                    ?? throw new NotFoundException($"Promotion {promotionId} not found");

        _mapper.Map(request, promo);
        await _uow.Promotions.UpdateAsync(promo, ct);

        _cache.RemoveGroup(OrdersCache.PromotionsGroup, "PromotionUpdated");
    }

    public async Task DeleteAsync(long promotionId, CancellationToken ct = default)
    {
        _ = await _uow.Promotions.GetByIdAsync(promotionId, ct)
            ?? throw new NotFoundException($"Promotion {promotionId} not found");

        await _uow.Promotions.DeleteAsync(promotionId, ct);
        _cache.RemoveGroup(OrdersCache.PromotionsGroup, "PromotionDeleted");
    }

    private static void ValidateDates(UpsertPromotionRequest request)
    {
        if (request.EndDate <= request.StartDate)
            throw new ValidationException("EndDate must be greater than StartDate");
    }
}
