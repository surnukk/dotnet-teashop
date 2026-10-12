using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orders.Domain.Models;

public class PaymentRecord
{
    public long OrderID { get; set; }
    public string PaymentMethod { get; set; } = "";
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime TransactionDate { get; set; }
}