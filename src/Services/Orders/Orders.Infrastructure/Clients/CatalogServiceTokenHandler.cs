using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Orders.Infrastructure.Clients;

/// <summary>
/// Obtém e CACHEIA um access token via client_credentials no Keycloak,
/// anexando "Authorization: Bearer" em toda chamada ao Catalog. O cache
/// evita pedir um token novo a cada requisição — só renova quando o
/// token em memória está a menos de 30s de expirar.
/// </summary>
public class CatalogServiceTokenHandler : DelegatingHandler
{
    private readonly HttpClient _tokenHttpClient;
    private readonly string _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiresAt = DateTimeOffset.MinValue;

    public CatalogServiceTokenHandler(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _tokenHttpClient = httpClientFactory.CreateClient("keycloak-token");
        _tokenEndpoint = configuration["Keycloak:TokenEndpoint"]
            ?? throw new InvalidOperationException("Keycloak:TokenEndpoint não configurado.");
        _clientId = configuration["Keycloak:OrdersClientId"]
            ?? throw new InvalidOperationException("Keycloak:OrdersClientId não configurado.");
        _clientSecret = configuration["Keycloak:OrdersClientSecret"]
            ?? throw new InvalidOperationException("Keycloak:OrdersClientSecret não configurado.");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedTokenExpiresAt)
            return _cachedToken;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedTokenExpiresAt)
                return _cachedToken;

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret,
            };

            using var response = await _tokenHttpClient.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(form), cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Resposta inesperada do Keycloak ao obter token de serviço.");

            _cachedToken = payload.AccessToken;
            _cachedTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn - 30);

            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private record KeycloakTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
