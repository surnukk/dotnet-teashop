using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orders.Bll.DTO;

namespace Orders.Bll.Services;

public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(long orderId, CancellationToken ct = default);
    Task<IEnumerable<OrderDto>> GetByCustomerIdAsync(long customerId, CancellationToken ct = default);
    Task<long> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default); Task ShipOrderAsync(long orderId, CancellationToken ct = default);
    Task ConfirmOrderAsync(long orderId, CancellationToken ct = default);

    Task CancelOrderAsync(long orderId, CancellationToken ct = default);
}