using AutoMapper;
using Microsoft.Data.SqlClient;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Dal.Interfaces;
using Orders.Domain.Exceptions;
using Orders.Domain.Models;
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
        await _uow.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        try
        {
            var customer = await _uow.Customers.GetByIdAsync(customerId, ct);
            if (customer is null)
                throw new NotFoundException($"Customer with ID {customerId} not found.");

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            foreach (var itemDto in items)
            {
                var product = await _uow.Products.GetByIdAsync(itemDto.ProductId, ct);
                if (product is null)
                    throw new NotFoundException($"Product with ID {itemDto.ProductId} not found.");

                decimal itemTotal = product.Price * itemDto.Units;
                totalAmount += itemTotal;

                orderItems.Add(new OrderItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Units = itemDto.Units
                });
            }

            var order = new Order
            {
                CustomerID = customerId,
                PromotionID = promotionId,
                OrderState = "Submitted",
                TotalAmount = totalAmount,
                OrderDate = DateTime.UtcNow
            };

            long orderId = await _uow.Orders.AddAsync(order, ct);

            foreach (var item in orderItems)
            {
                item.OrderID = orderId;
                await _uow.Orders.AddItemAsync(item, ct);
            }

            await _uow.CommitAsync(ct);
            return orderId;
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
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
        await _uow.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
        try
        {
            await _uow.Orders.CancelAsync(orderId, ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}