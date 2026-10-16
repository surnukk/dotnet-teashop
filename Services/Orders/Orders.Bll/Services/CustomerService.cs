using AutoMapper;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;

namespace Orders.Bll.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<CustomerDto?> GetByIdAsync(long customerId, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(customerId, ct);
        return customer == null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByEmailAsync(email, ct);
        return customer == null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await _uow.Customers.GetAllAsync(ct);
        return _mapper.Map<IEnumerable<CustomerDto>>(customers);
    }

    public async Task<long> CreateCustomerAsync(UpsertCustomerRequest request, CancellationToken ct = default)
    {
        var customer = _mapper.Map<Customer>(request);
        return await _uow.Customers.AddAsync(customer, ct);
    }

    public async Task UpdateAsync(long customerId, UpsertCustomerRequest request, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(customerId, ct)
                       ?? throw new NotFoundException($"Customer {customerId} not found");

        _mapper.Map(request, customer);
        await _uow.Customers.UpdateAsync(customer, ct);
    }

    public async Task DeleteAsync(long customerId, CancellationToken ct = default)
    {
        _ = await _uow.Customers.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException($"Customer {customerId} not found");

        await _uow.Customers.DeleteAsync(customerId, ct);
    }
}