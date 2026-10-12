using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orders.Bll.DTO
{
    public record ProductDto(long ProductID, string ProductName, decimal Price);

    public class UpsertProductRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string ProductName { get; set; } = string.Empty;

        [Range(0.01, 1000000.00)]
        public decimal Price { get; set; }
    }
}
