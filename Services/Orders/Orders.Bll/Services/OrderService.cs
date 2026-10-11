using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orders.Dal;

namespace Orders.Bll.Services;

public sealed class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;

    public OrderService(IUnitOfWork uow)
    {
        _uow = uow;
    }
}