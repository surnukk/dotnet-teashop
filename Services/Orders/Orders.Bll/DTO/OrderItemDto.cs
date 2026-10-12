using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orders.Bll.DTO
{
    public record OrderItemDto(long OrderID, long ProductId, string ProductName, decimal UnitPrice, int Units);

    public class CreateOrderItemRequest
    {
        [Range(1, long.MaxValue)]
        public long ProductId { get; set; }

        [Range(1, 1000)]
        public int Units { get; set; }
    }
}
