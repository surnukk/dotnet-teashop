using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Catalog.Bll.DTO;
using Catalog.Domain;

namespace Catalog.Bll.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // товари
        CreateMap<Product, ProductDto>();
        CreateMap<ProductDetail, ProductDetailDto>();
        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<Product, ProductDetailsDto>()
            .ForCtorParam("CategoryName", o => o.MapFrom(p => p.Category.CategoryName))
            .ForCtorParam("Tags", o => o.MapFrom(p => p.ProductTags.Select(pt => pt.Tag)));

        // категорії
        CreateMap<Category, CategoryDto>();
        CreateMap<Category, CategoryWithProductsDto>();

        // теги
        CreateMap<Tag, TagDto>();
    }
}