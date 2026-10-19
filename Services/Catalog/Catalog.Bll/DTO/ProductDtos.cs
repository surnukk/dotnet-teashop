using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Catalog.Bll.DTO;

public record ProductDto(int ProductId, int CategoryId, string ProductName,
                         decimal Price, int StockQuantity, bool IsAvailable);

public record ProductDetailDto(string? Descrip, string OriginCountry, string? Region,
                               string TeaForm, string TeaVariety, string? FlavorNotes,
                               int WeightGrams);

public record ProductImageDto(int ImageId, string ImageUrl, bool IsMain);

public record ProductDetailsDto(int ProductId, string ProductName, string CategoryName,
                                decimal Price, int StockQuantity, bool IsAvailable,
                                ProductDetailDto? Detail,
                                List<ProductImageDto> Images,
                                List<TagDto> Tags);

public record CreateProductDto(int CategoryId, string ProductName, decimal Price);

public record ChangePriceDto(
    [Range(typeof(decimal), "0.01", "1000000")] decimal Price);

public record StockChangeDto(
    [Range(1, 100000)] int Quantity);