using AutoMapper;
using Microsoft.Data.SqlClient;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using System.Data;

namespace Orders.Bll.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public OrderService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<OrderDto?> GetByIdAsync(long orderId, CancellationToken ct = default)
    {
        var order = await _uow.Orders.GetWithItemsAsync(orderId, ct);
        return order == null ? null : _mapper.Map<OrderDto>(order);
    }

    public async Task<IEnumerable<OrderDto>> GetByCustomerIdAsync(long customerId, CancellationToken ct = default)
    {
        var orders = await _uow.Orders.GetByCustomerAsync(customerId, ct);
        return _mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task<long> CreateOrderAsync(long customerId, long? promotionId, IEnumerable<OrderItemDto> items, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task ConfirmOrderAsync(long orderId, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var order = await _uow.Orders.GetWithItemsAsync(orderId, ct)
                        ?? throw new NotFoundException($"Order {orderId} not found");

            if (order.Items == null || order.Items.Count == 0)
                throw new BusinessConflictException($"Order {orderId} has no items");

            await _uow.Orders.ConfirmAsync(orderId, ct);

            await _uow.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            await _uow.RollbackAsync(ct);
            throw new NotFoundException($"Order {orderId} not found");
        }
        catch (SqlException ex) when (ex.Number == 50002 || ex.Number == 50003)
        {
            await _uow.RollbackAsync(ct);
            throw new BusinessConflictException(ex.Message);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task CancelOrderAsync(long orderId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}