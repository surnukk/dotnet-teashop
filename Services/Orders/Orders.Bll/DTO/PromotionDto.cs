using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orders.Bll.DTO
{
    public record PromotionDto(long PromotionID, string Code,
        decimal DiscountPercentage, DateTime StartDate, DateTime EndDate, bool IsActive);

    public class UpsertPromotionRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Code { get; set; } = string.Empty;

        [Range(0.01, 100.00)]
        public decimal DiscountPercentage { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
