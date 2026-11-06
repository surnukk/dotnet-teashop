using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Orders.Bll.DTO;
using Orders.Bll.Services;
using Orders.Contracts;
using Orders.Domain.Exceptions;

namespace Orders.Api.GrpcServices;

/// <summary>gRPC-адаптер над BLL замовлень (той самий IOrderService, що й у REST-контролері).</summary>
public class OrdersGrpcService(IOrderService orders, ILogger<OrdersGrpcService> logger)
    : OrdersService.OrdersServiceBase
{
    public override Task<OrderReply> GetOrder(GetOrderRequest request, ServerCallContext context)
        => GrpcCall.RunAsync(nameof(GetOrder), context, logger, async ct =>
        {
            if (request.Id <= 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));

            var order = await orders.GetByIdAsync(request.Id, ct)
                        ?? throw new NotFoundException($"Order {request.Id} not found");

            return ToReply(order);
        });

    private static OrderReply ToReply(OrderDto o)
    {
        var reply = new OrderReply
        {
            OrderId = o.OrderID,
            CustomerId = o.CustomerID,
            OrderDate = Timestamp.FromDateTime(DateTime.SpecifyKind(o.OrderDate, DateTimeKind.Utc)),
            OrderState = o.OrderState,
            TotalAmount = (double)o.TotalAmount
        };

        if (o.PromotionID is { } promotionId)
            reply.PromotionId = promotionId;

        reply.Items.AddRange(o.Items.Select(i => new OrderItemReply
        {
            OrderId = i.OrderID,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = (double)i.UnitPrice,
            Units = i.Units
        }));

        return reply;
    }
}