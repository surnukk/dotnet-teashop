using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orders.Bll.DTO
{
    public record PaymentRecordDto(long OrderID, string PaymentMethod, decimal Amount, string PaymentStatus, DateTime TransactionDate);

    public class ProcessPaymentRequest
    {
        [Range(1, long.MaxValue)]
        public long OrderId { get; set; }

        [Range(0.01, 1000000.00)]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}
