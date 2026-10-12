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

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(long customerId, CancellationToken ct = default);
    Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<long> CreateCustomerAsync(UpsertCustomerRequest request, CancellationToken ct = default);
    Task UpdateAsync(long customerId, UpsertCustomerRequest request, CancellationToken ct = default);
    Task DeleteAsync(long customerId, CancellationToken ct = default);
}