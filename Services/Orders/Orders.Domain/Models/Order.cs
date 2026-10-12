using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orders.Domain.Models;

public class Order
{
    public long OrderID { get; set; }
    public long CustomerID { get; set; }
    public long? PromotionID { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderState { get; set; } = "Submitted";
    public decimal TotalAmount { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
