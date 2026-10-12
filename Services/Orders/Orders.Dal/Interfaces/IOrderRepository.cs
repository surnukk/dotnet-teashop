using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(long customerId, CancellationToken ct = default);
    Task AddItemAsync(OrderItem item, CancellationToken ct = default);

    Task ConfirmAsync(long orderId, CancellationToken ct = default);         
    Task MarkAsPaidAsync(long orderId, string paymentMethod, decimal amount,
        CancellationToken ct = default);                    
    Task ShipAsync(long orderId, CancellationToken ct = default);   
    Task CancelAsync(long orderId, CancellationToken ct = default);   
}