using Catalog.Bll.DTO;
using Catalog.Bll.Services;
using Catalog.Contracts;
using Catalog.Domain;
using Grpc.Core;

namespace Catalog.Api.GrpcServices;

/// <summary>gRPC-адаптер над BLL Каталогу (ті самі сервіси, що використовують REST-контролери).</summary>
public class CatalogGrpcService(IProductService products, ILogger<CatalogGrpcService> logger)
    : CatalogService.CatalogServiceBase
{
    private const int MaxBatchSize = 100;

    public override Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
        => GrpcCall.RunAsync(nameof(GetProduct), context, logger, async ct =>
        {
            var id = ValidateId(request.Id);

            var dto = await products.GetByIdAsync(id, ct);
            return ToReply(dto);
        });

    public override Task<ProductsReply> GetProductsByIds(GetProductsByIdsRequest request, ServerCallContext context)
        => GrpcCall.RunAsync(nameof(GetProductsByIds), context, logger, async ct =>
        {
            if (request.Ids.Count == 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Ids must not be empty"));
            if (request.Ids.Count > MaxBatchSize)
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"At most {MaxBatchSize} ids per call"));

            var ids = request.Ids.Select(ValidateId).Distinct().ToList();
            var reply = new ProductsReply();

            foreach (var id in ids)
            {
                try
                {
                    reply.Products.Add(ToReply(await products.GetByIdAsync(id, ct)));
                }
                catch (EntityNotFoundException)
                {
                    logger.LogDebug("Product {ProductId} not found, skipped in batch response", id);
                }
            }

            return reply;
        });

    private static int ValidateId(long id)
    {
        if (id <= 0 || id > int.MaxValue)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be a positive 32-bit integer"));
        return (int)id;
    }

    private static ProductReply ToReply(ProductDetailsDto p) => new()
    {
        Id = p.ProductId,
        Name = p.ProductName,
        CategoryName = p.CategoryName,
        Price = (double)p.Price,
        StockQuantity = p.StockQuantity,
        IsAvailable = p.IsAvailable
    };
}