using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Orders.Bll.DTO;
using Orders.Domain.Models;

namespace Orders.Bll.Mappings;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Customer, CustomerDto>();
        CreateMap<Product, ProductDto>();
        CreateMap<Promotion, PromotionDto>();
        CreateMap<PaymentRecord, PaymentRecordDto>();
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<Order, OrderDto>();

        CreateMap<UpsertCustomerRequest, Customer>()
            .ForMember(d => d.CustomerID, o => o.Ignore())
            .ForMember(d => d.IsActive, o => o.Ignore());
    }
}