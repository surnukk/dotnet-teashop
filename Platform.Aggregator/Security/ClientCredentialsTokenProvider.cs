using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Platform.Aggregator.Security;

public interface IClientTokenProvider
{
    Task<string> GetTokenAsync(CancellationToken ct = default);
}

/// <summary>Client Credentials flow: бере токен у IdentityServer і кешує до закінчення терміну дії.</summary>
public sealed class ClientCredentialsTokenProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ClientCredentialsTokenProvider> logger) : IClientTokenProvider
{
    public const string HttpClientName = "identityserver-token";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt)
            return _accessToken;

        await _lock.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt)
                return _accessToken;

            var authority = (configuration["IdentityServer:Authority"] ?? "http://localhost:5200").TrimEnd('/');
            var clientId = configuration["IdentityServer:ClientId"]
                           ?? throw new InvalidOperationException("IdentityServer:ClientId is missing");
            var clientSecret = configuration["IdentityServer:ClientSecret"]
                               ?? throw new InvalidOperationException("IdentityServer:ClientSecret is missing");
            var scope = configuration["IdentityServer:Scope"] ?? "orders.read products.read";

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = scope
            };

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsync($"{authority}/connect/token", new FormUrlEncodedContent(form), ct);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<IdentityServerTokenResponse>(ct)
                        ?? throw new InvalidOperationException("Empty token response from IdentityServer");

            _accessToken = token.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(token.ExpiresIn - 60, 30));   // запас 60 с

            logger.LogInformation(
                "Obtained client-credentials token for {ClientId} (scope: {Scope}), expires in {Seconds}s",
                clientId, scope, token.ExpiresIn);

            return _accessToken;
        }
        finally
        {
            _lock.Release();
        }
    }
}

internal sealed record IdentityServerTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);