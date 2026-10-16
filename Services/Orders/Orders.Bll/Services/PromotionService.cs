using AutoMapper;
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

    public async Task<long> CreateAsync(UpsertPromotionRequest request, CancellationToken ct = default)
    {
        ValidateDates(request);
        var promo = _mapper.Map<Promotion>(request);
        return await _uow.Promotions.AddAsync(promo, ct);
    }

    public async Task UpdateAsync(long promotionId, UpsertPromotionRequest request, CancellationToken ct = default)
    {
        ValidateDates(request);

        var promo = await _uow.Promotions.GetByIdAsync(promotionId, ct)
                    ?? throw new NotFoundException($"Promotion {promotionId} not found");

        _mapper.Map(request, promo);
        await _uow.Promotions.UpdateAsync(promo, ct);
    }

    public async Task DeleteAsync(long promotionId, CancellationToken ct = default)
    {
        _ = await _uow.Promotions.GetByIdAsync(promotionId, ct)
            ?? throw new NotFoundException($"Promotion {promotionId} not found");

        await _uow.Promotions.DeleteAsync(promotionId, ct);
    }

    private static void ValidateDates(UpsertPromotionRequest request)
    {
        if (request.EndDate <= request.StartDate)
            throw new ValidationException("EndDate must be greater than StartDate");
    }
}