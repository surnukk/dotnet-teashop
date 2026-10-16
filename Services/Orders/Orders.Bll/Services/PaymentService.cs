using AutoMapper;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public PaymentService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PaymentRecordDto?> GetByOrderIdAsync(long orderId, CancellationToken ct = default)
    {
        var payment = await _uow.Payments.GetByOrderIdAsync(orderId, ct);
        return payment == null ? null : _mapper.Map<PaymentRecordDto>(payment);
    }

    public async Task ProcessPaymentAsync(long orderId, decimal amount, string paymentMethod, CancellationToken ct = default)
    {
        await _uow.Orders.MarkAsPaidAsync(orderId, paymentMethod, amount, ct);
    }

    public async Task RefundPaymentAsync(long orderId, CancellationToken ct = default)
    {
        var payment = await _uow.Payments.GetByOrderIdAsync(orderId, ct)
                      ?? throw new NotFoundException($"Payment for order {orderId} not found");

        if (payment.PaymentStatus != "Completed")
            throw new BusinessConflictException("Only a completed payment can be refunded");

        payment.PaymentStatus = "Refunded";
        await _uow.Payments.UpdateAsync(payment, ct);
    }
}