using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.DTO;

public record CategoryDto(int CategoryId, string CategoryName, string? Descrip);

// категорія з товарами (Explicit Loading)
public record CategoryWithProductsDto(int CategoryId, string CategoryName, string? Descrip,
                                      List<ProductDto> Products);

public record CreateCategoryDto(
    [Required, StringLength(100)] string CategoryName,
    [StringLength(500)] string? Descrip);

public record UpdateCategoryDto(
    [Required, StringLength(100)] string CategoryName,
    [StringLength(500)] string? Descrip);