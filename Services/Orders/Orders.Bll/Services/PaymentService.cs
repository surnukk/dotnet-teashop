using AutoMapper;
using Microsoft.Extensions.Logging;
using Orders.Bll.Caching;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;
using System.Diagnostics;

namespace Orders.Bll.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;
    private readonly ICacheService _cache;

    public PaymentService(IUnitOfWork uow, IMapper mapper, ILogger<PaymentService> logger, ICacheService cache)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public Task<PaymentRecordDto?> GetByOrderIdAsync(long orderId, CancellationToken ct = default)
        => _cache.GetOrCreateAsync<PaymentRecordDto>(OrdersCache.Payment(orderId), async token =>
        {
            var payment = await _uow.Payments.GetByOrderIdAsync(orderId, token);
            return payment == null ? null : _mapper.Map<PaymentRecordDto>(payment);
        }, OrdersCachePolicies.Payment, ct);

    public async Task ProcessPaymentAsync(long orderId, decimal amount, string paymentMethod, CancellationToken ct = default)
    {
        await _uow.Orders.MarkAsPaidAsync(orderId, paymentMethod, amount, ct);

        await _cache.InvalidateOrderAsync(orderId, "OrderPaid");

        _logger.LogInformation("Order {OrderId} paid: amount {Amount}, method {PaymentMethod}", orderId, amount, paymentMethod);
    }

    public async Task RefundPaymentAsync(long orderId, CancellationToken ct = default)
    {
        var payment = await _uow.Payments.GetByOrderIdAsync(orderId, ct)
                      ?? throw new NotFoundException($"Payment for order {orderId} not found");

        if (payment.PaymentStatus != "Completed")
            throw new BusinessConflictException("Only a completed payment can be refunded");

        payment.PaymentStatus = "Refunded";
        await _uow.Payments.UpdateAsync(payment, ct);

        await _cache.InvalidateOrderAsync(orderId, "PaymentRefunded");

        _logger.LogInformation("Payment for order {OrderId} refunded", orderId);
    }
}
