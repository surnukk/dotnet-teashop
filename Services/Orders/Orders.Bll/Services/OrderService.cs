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

    public async Task<long> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default)
    {
        if (request.Items.Count == 0)
            throw new ValidationException("Order must contain at least one item");

        var duplicate = request.Items.GroupBy(i => i.ProductId).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new ValidationException($"Product {duplicate.Key} is listed more than once");

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct)
                           ?? throw new NotFoundException($"Customer {request.CustomerId} not found");
            if (!customer.IsActive)
                throw new BusinessConflictException($"Customer {request.CustomerId} is not active");

            decimal discountPercent = 0;
            if (request.PromotionId is { } promotionId)
            {
                var promo = await _uow.Promotions.GetByIdAsync(promotionId, ct)
                            ?? throw new ValidationException($"Promotion {promotionId} does not exist");

                var now = DateTime.UtcNow;
                if (!promo.IsActive || now < promo.StartDate || now > promo.EndDate)
                    throw new ValidationException($"Promotion '{promo.Code}' is not active");

                discountPercent = promo.DiscountPercentage;
            }

            var ids = request.Items.Select(i => i.ProductId).ToList();
            var products = (await _uow.Products.GetByIdsAsync(ids, ct)).ToDictionary(p => p.ProductID);

            var missing = ids.Where(id => !products.ContainsKey(id)).ToList();
            if (missing.Count > 0)
                throw new NotFoundException($"Products not found: {string.Join(", ", missing)}");

            var items = request.Items.Select(i => new OrderItem
            {
                ProductID = i.ProductId,
                ProductName = products[i.ProductId].ProductName,
                UnitPrice = products[i.ProductId].Price,
                Units = i.Units
            }).ToList();

            var subtotal = items.Sum(i => i.UnitPrice * i.Units);
            var total = Math.Round(subtotal * (100m - discountPercent) / 100m, 2, MidpointRounding.AwayFromZero);

            var order = new Order
            {
                CustomerID = request.CustomerId,
                PromotionID = request.PromotionId,
                OrderState = "Submitted",
                TotalAmount = total
            };

            var orderId = await _uow.Orders.AddAsync(order, ct);
            foreach (var item in items)
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

            if (order.Items.Count == 0)
                throw new BusinessConflictException($"Order {orderId} has no items");

            await _uow.Orders.ConfirmAsync(orderId, ct);
            await _uow.CommitAsync(ct);
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

    public async Task ShipOrderAsync(long orderId, CancellationToken ct = default)
    {
        await _uow.Orders.ShipAsync(orderId, ct);
    }
}