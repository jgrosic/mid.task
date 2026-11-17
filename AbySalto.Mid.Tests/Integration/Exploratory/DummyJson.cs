using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AbySalto.Mid.Tests.Integration.Exploratory;

[Explicit] // Only runs when explicitly selected
internal class DummyJson
{
    // /docs/products is documentation (HTML), not the JSON API.
    // Keep the exploratory output small; fetching all products will use limit=0 later.
    private const string ProductsUrl = "https://dummyjson.com/products?limit=3";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [Test]
    public async Task Products_PrintFullJsonResponse()
    {
        // arrange
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        // act
        using var response = await client.GetAsync(ProductsUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        
        using var document = JsonDocument.Parse(json);

        await TestContext.Current!.OutputWriter.WriteLineAsync(
            JsonSerializer.Serialize(document.RootElement, JsonOptions));

        // assert
        var products = document.RootElement.GetProperty("products");
    }

    [Test]
    public async Task Products_ExtractOnlyIdTitleAndPrice()
    {
        // arrange
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        // act
        var response = await client.GetFromJsonAsync<ProductsResponse>(ProductsUrl, JsonOptions);

        // assert
        await Assert.That(response).IsNotNull();

        await TestContext.Current!.OutputWriter.WriteLineAsync(
            JsonSerializer.Serialize(response!.Products, JsonOptions));
    }

    // Extra fields in the upstream JSON are ignored during deserialization.
    private sealed record ProductsResponse([property: JsonRequired] ProductInfo[] Products);

    private sealed record ProductInfo(
        [property: JsonRequired] int Id,
        [property: JsonRequired] string Title,
        [property: JsonRequired] decimal Price);
}
