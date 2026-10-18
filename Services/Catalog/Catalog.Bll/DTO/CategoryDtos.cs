using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Catalog.Bll.DTO;

public record CategoryDto(int CategoryId, string CategoryName, string? Descrip);

// категорія з товарами (Explicit Loading)
public record CategoryWithProductsDto(int CategoryId, string CategoryName, string? Descrip,
                                      List<ProductDto> Products);

public record CreateCategoryDto(string CategoryName, string? Descrip);
public record UpdateCategoryDto(string CategoryName, string? Descrip);