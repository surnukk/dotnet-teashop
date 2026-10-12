using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orders.Bll.DTO
{
    public record OrderDto(long OrderID, long CustomerID, long? PromotionID, DateTime OrderDate,
        string OrderState, decimal TotalAmount, IReadOnlyList<OrderItemDto> Items);

    public class CreateOrderRequest
    {
        [Range(1, long.MaxValue)]
        public long CustomerId { get; set; }

        public long? PromotionId { get; set; }

        [Required]
        [MinLength(1)]
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }
}
