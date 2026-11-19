using System.Net;
using System.Net.Http.Json;
using System.Text;
using AbySalto.Mid.Application.Products;
using AbySalto.Mid.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AbySalto.Mid.Tests.Api;

internal class BasketTests
{
    private const string Password = "Password123!";
    private readonly List<string> _emails = [];
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;
    private TestProductCatalog _catalog = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        await ApiFactory.InitializeDatabaseAsync();

        _catalog = new TestProductCatalog();
        _factory = new ApiFactory(services =>
        {
            // Keep the real basket service and SQL Server persistence.
            services.RemoveAll<IProductCatalog>();
            services.AddSingleton<IProductCatalog>(_catalog);
        });

        _client = _factory.CreateClient(ClientOptions());
        await RegisterAndLoginAsync(_client);
    }

    [After(Test)]
    public async Task CleanUp()
    {
        if (_factory is null) return;

        try
        {
            // Database access is only for cleanup, never for basket assertions.
            // Deleting our users cascades to their baskets and items.
            if (_emails.Count > 0)
            {
                await using var db = ApiFactory.CreateDbContext();
                await db.Users.Where(user => _emails.Contains(user.Email!)).ExecuteDeleteAsync();
            }
        }
        finally
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }
    }

    [Test]
    [Arguments("GET", "/api/basket")]
    [Arguments("POST", "/api/basket/items/42")]
    [Arguments("DELETE", "/api/basket/items/42")]
    public async Task BasketWithoutLogin_ReturnsUnauthorized(string method, string path)
    {
        // arrange
        using var anonymousClient = _factory.CreateClient(ClientOptions());
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST")
            request.Content = JsonContent("""{"quantity": 1}""");

        // act
        using var response = await anonymousClient.SendAsync(request);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetForNewUser_ReturnsEmptyBasket()
    {
        // arrange
        // Setup registered a new user without adding any items.

        // act
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(basket.Items.Length).IsEqualTo(0);
        await Assert.That(basket.TotalPrice).IsEqualTo(0m);
    }

    [Test]
    public async Task AddProduct_ReturnsNoContentAndBasketWithSnapshotAndTotals()
    {
        // arrange
        const string json = """{"quantity": 2}""";

        // act
        using var response = await PostItemAsync(42, json);
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(basket.Items.Length).IsEqualTo(1);
        var item = basket.Items.Single();
        await Assert.That(item.ProductId).IsEqualTo(42);
        await Assert.That(item.ProductTitle).IsEqualTo("Test product");
        await Assert.That(item.UnitPrice).IsEqualTo(12.50m);
        await Assert.That(item.Quantity).IsEqualTo(2);
        await Assert.That(item.TotalPrice).IsEqualTo(25m);
        await Assert.That(basket.TotalPrice).IsEqualTo(25m);
    }

    [Test]
    public async Task ReAddProduct_IncreasesQuantityAndKeepsOriginalSnapshot()
    {
        // arrange
        await AddItemAsync(42, 2);
        _catalog.Product = new ProductDto(42, "Changed title", 99m);

        // act
        using var response = await PostItemAsync(42, """{"quantity": 3}""");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(basket.Items.Length).IsEqualTo(1);
        var item = basket.Items.Single();
        await Assert.That(item.Quantity).IsEqualTo(5);
        await Assert.That(item.ProductTitle).IsEqualTo("Test product");
        await Assert.That(item.UnitPrice).IsEqualTo(12.50m);
        await Assert.That(item.TotalPrice).IsEqualTo(62.50m);
        await Assert.That(basket.TotalPrice).IsEqualTo(62.50m);
    }

    [Test]
    public async Task ReAddProduct_WhenCatalogIsUnavailable_StillSucceeds()
    {
        // arrange
        await AddItemAsync(42, 1);
        _catalog.IsUnavailable = true;

        // act
        using var response = await PostItemAsync(42, """{"quantity": 1}""");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(basket.Items.Single().Quantity).IsEqualTo(2);
    }

    [Test]
    public async Task AddDifferentProducts_ReturnsSeparateItemsAndCombinedTotal()
    {
        // arrange
        await AddItemAsync(42, 2);

        // act
        await AddItemAsync(43, 3);
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(basket.Items.Length).IsEqualTo(2);
        await Assert.That(basket.Items.Single(item => item.ProductId == 42).Quantity).IsEqualTo(2);
        await Assert.That(basket.Items.Single(item => item.ProductId == 43).Quantity).IsEqualTo(3);
        await Assert.That(basket.TotalPrice).IsEqualTo(37m);
    }

    [Test]
    [Arguments("""{"quantity": 0}""")]
    [Arguments("""{"quantity": -1}""")]
    [Arguments("""{"quantity": 101}""")]
    [Arguments("""{}""")]
    public async Task AddInvalidQuantity_ReturnsBadRequestWithoutChangingBasket(string json)
    {
        // arrange
        await AddItemAsync(42, 2);

        // act
        using var response = await PostItemAsync(42, json);
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(basket.Items.Single().Quantity).IsEqualTo(2);
        await Assert.That(basket.TotalPrice).IsEqualTo(25m);
    }

    [Test]
    public async Task AddUpToLimit_SucceedsButExceedingLimitLeavesBasketUnchanged()
    {
        // arrange
        await AddItemAsync(42, 99);

        // act
        using var accepted = await PostItemAsync(42, """{"quantity": 1}""");
        using var rejected = await PostItemAsync(42, """{"quantity": 1}""");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(accepted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(rejected.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(basket.Items.Single().Quantity).IsEqualTo(100);
        await Assert.That(basket.TotalPrice).IsEqualTo(1250m);
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    public async Task AddAndRemoveInvalidProductId_ReturnBadRequest(int productId)
    {
        // arrange
        await AddItemAsync(42, 1);

        // act
        using var added = await PostItemAsync(productId, """{"quantity": 1}""");
        using var removed = await _client.DeleteAsync($"/api/basket/items/{productId}");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(added.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(removed.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(basket.Items.Single().ProductId).IsEqualTo(42);
    }

    [Test]
    public async Task AddMissingProduct_ReturnsNotFoundWithoutAddingItem()
    {
        // arrange
        await AddItemAsync(42, 1);

        // act
        using var response = await PostItemAsync(999999, """{"quantity": 1}""");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(basket.Items.Length).IsEqualTo(1);
        await Assert.That(basket.Items.Single().ProductId).IsEqualTo(42);
    }

    [Test]
    public async Task AddWhenCatalogIsUnavailable_ReturnsBadGatewayAndEmptyBasket()
    {
        // arrange
        _catalog.IsUnavailable = true;

        // act
        using var response = await PostItemAsync(42, """{"quantity": 1}""");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadGateway);
        await Assert.That(basket.Items.Length).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveProduct_RemovesWholeItemAndKeepsOtherItems()
    {
        // arrange
        await AddItemAsync(42, 5);
        await AddItemAsync(43, 2);

        // act
        using var response = await _client.DeleteAsync("/api/basket/items/42");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(basket.Items.Length).IsEqualTo(1);
        await Assert.That(basket.Items.Single().ProductId).IsEqualTo(43);
        await Assert.That(basket.Items.Single().Quantity).IsEqualTo(2);
        await Assert.That(basket.TotalPrice).IsEqualTo(8m);
    }

    [Test]
    public async Task RemoveLastProductAndRepeat_ReturnsNoContentAndEmptyBasket()
    {
        // arrange
        await AddItemAsync(42, 5);

        // act
        using var first = await _client.DeleteAsync("/api/basket/items/42");
        using var second = await _client.DeleteAsync("/api/basket/items/42");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(basket.Items.Length).IsEqualTo(0);
        await Assert.That(basket.TotalPrice).IsEqualTo(0m);
    }

    [Test]
    public async Task RemoveFromNewUsersBasket_ReturnsNoContent()
    {
        // arrange
        // This user has never added a product.

        // act
        using var response = await _client.DeleteAsync("/api/basket/items/42");
        var basket = await GetBasketAsync(_client);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(basket.Items.Length).IsEqualTo(0);
    }

    [Test]
    public async Task DifferentUsers_CannotReadOrChangeEachOthersBasket()
    {
        // arrange
        await AddItemAsync(42, 2);
        using var otherClient = _factory.CreateClient(ClientOptions());
        await RegisterAndLoginAsync(otherClient);

        // act
        var otherBefore = await GetBasketAsync(otherClient);
        using var removal = await otherClient.DeleteAsync("/api/basket/items/42");
        using var addition = await otherClient.PostAsJsonAsync(
            "/api/basket/items/42", new { quantity = 1 });
        var original = await GetBasketAsync(_client);
        var otherAfter = await GetBasketAsync(otherClient);

        // assert
        await Assert.That(otherBefore.Items.Length).IsEqualTo(0);
        await Assert.That(removal.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(addition.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(original.Items.Single().Quantity).IsEqualTo(2);
        await Assert.That(otherAfter.Items.Single().Quantity).IsEqualTo(1);
    }

    private async Task RegisterAndLoginAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        _emails.Add(email);
        var credentials = new { email, password = Password };

        using var registration = await client.PostAsJsonAsync("/api/auth/register", credentials);
        registration.EnsureSuccessStatusCode();
        using var login = await client.PostAsJsonAsync("/api/auth/login?useCookies=true", credentials);
        login.EnsureSuccessStatusCode();
    }

    private async Task AddItemAsync(int productId, int quantity)
    {
        using var response = await _client.PostAsJsonAsync(
            $"/api/basket/items/{productId}", new { quantity });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    private async Task<HttpResponseMessage> PostItemAsync(int productId, string json)
    {
        using var content = JsonContent(json);

        return await _client.PostAsync($"/api/basket/items/{productId}", content);
    }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

    private static async Task<BasketResponse> GetBasketAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/basket");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<BasketResponse>()
               ?? throw new InvalidOperationException("The API returned no basket JSON.");
    }

    private static WebApplicationFactoryClientOptions ClientOptions() => new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    };

    // Read totals from the actual response, rather than recomputing them with application DTOs.
    private sealed record BasketResponse(BasketItemResponse[] Items, decimal TotalPrice);

    private sealed record BasketItemResponse(
        int ProductId,
        string ProductTitle,
        decimal UnitPrice,
        int Quantity,
        decimal TotalPrice);

    private sealed class TestProductCatalog : IProductCatalog
    {
        public ProductDto Product { get; set; } = new(42, "Test product", 12.50m);
        public bool IsUnavailable { get; set; }

        public Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Basket endpoints only look up individual products.");

        public Task<ProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsUnavailable)
                throw new ExternalServiceException("The test catalog is unavailable.");

            ProductDto? product = productId switch
            {
                42 => Product,
                43 => new ProductDto(43, "Second product", 4m),
                _ => null
            };

            return Task.FromResult(product);
        }
    }
}