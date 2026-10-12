using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Orders.Bll.DTO;

namespace Orders.Bll.Services;

public interface IPaymentService
{
    Task<PaymentRecordDto?> GetByOrderIdAsync(long orderId, CancellationToken ct = default);

    Task ProcessPaymentAsync(long orderId, decimal amount, string paymentMethod, CancellationToken ct = default);

    Task RefundPaymentAsync(long orderId, CancellationToken ct = default);
}