using Grpc.Core;

namespace Platform.Aggregator.Security;

public static class ClientCredentialsExtensions
{
    /// <summary>Додає Bearer-токен Client Credentials у metadata кожного gRPC-виклику.</summary>
    public static IHttpClientBuilder AddClientCredentialsToken(this IHttpClientBuilder builder) =>
        builder.AddCallCredentials(async (context, metadata, serviceProvider) =>
        {
            var provider = serviceProvider.GetRequiredService<IClientTokenProvider>();
            var token = await provider.GetTokenAsync();
            metadata.Add("Authorization", $"Bearer {token}");
        });
}