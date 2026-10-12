using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orders.Dal.Interfaces;
using Orders.Domain.Models;

namespace Orders.Dal.Interfaces;

public interface IPaymentRepository : IGenericRepository<PaymentRecord>
{
    Task<PaymentRecord?> GetByOrderIdAsync(long orderId, CancellationToken ct = default);
}
