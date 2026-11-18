using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AbySalto.Mid.Application.Products;

namespace AbySalto.Mid.Infrastructure.Products;

public sealed class DummyJsonProductClient(HttpClient httpClient) : IProductCatalog
{
    private sealed record ProductsResponse([property: JsonRequired] ProductDto[] Products);

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        CancellationToken cancellationToken)
    {
        // Without limit=0 DummyJSON returns only the default page, not the full catalog.
        using var response = await httpClient.GetAsync("products?limit=0", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProductsResponse>(
            cancellationToken: cancellationToken);

        return result?.Products
               ?? throw new JsonException("DummyJSON returned an invalid product list.");
    }

    public async Task<ProductDto?> GetProductAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        using var response = await httpClient.GetAsync($"products/{productId}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(
            cancellationToken: cancellationToken);

        return product
               ?? throw new JsonException("DummyJSON returned an invalid product.");
    }
}