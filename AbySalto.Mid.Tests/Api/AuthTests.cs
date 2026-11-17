using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AbySalto.Mid.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace AbySalto.Mid.Tests.Api;

internal class AuthTests
{
    private const string Password = "Password123!";
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;
    private string _email = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        await ApiFactory.InitializeDatabaseAsync();

        _email = $"{Guid.NewGuid():N}@example.com";
        _factory = new ApiFactory();
        _client = _factory.CreateClient(ClientOptions());
    }

    [After(Test)]
    public async Task CleanUp()
    {
        if (_factory is null) return;

        try
        {
            // Never clear the shared database: remove only this test's user.
            await using AppDbContext db = ApiFactory.CreateDbContext();
            await db.Users.Where(user => user.Email == _email).ExecuteDeleteAsync();
        }
        finally
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }
    }

    [Test]
    public async Task Register_ReturnsOk()
    {
        // arrange
        var credentials = new { email = _email, password = Password };

        // act
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", credentials);

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task InfoAfterRegistrationBeforeLogin_ReturnsUnauthorized()
    {
        // arrange
        await RegisterAsync();

        // act
        using HttpResponseMessage response = await _client.GetAsync("/api/auth/manage/info");

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task LoginWithCookies_ReturnsOkAndAuthenticationCookie()
    {
        // arrange
        await RegisterAsync();

        // act
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login?useCookies=true",
            new { email = _email, password = Password });

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        bool hasAuthenticationCookie =
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies)
            && cookies.Any(cookie => cookie.StartsWith(".AspNetCore.Identity.Application=", StringComparison.Ordinal));

        await Assert.That(hasAuthenticationCookie).IsTrue();
    }

    [Test]
    public async Task InfoAfterLogin_ReturnsCurrentUsersEmail()
    {
        // arrange
        await RegisterAsync();
        await LoginAsync();

        // act
        using HttpResponseMessage response = await _client.GetAsync("/api/auth/manage/info");

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        await Assert.That(body.RootElement.GetProperty("email").GetString()).IsEqualTo(_email);
    }

    [Test]
    public async Task LoginWithIncorrectPassword_ReturnsUnauthorizedEvenWhenAlreadyLoggedIn()
    {
        // arrange
        await RegisterAsync();
        await LoginAsync();

        // act
        using HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/auth/login?useCookies=true",
            new { email = _email, password = "WrongPassword123!" });

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InfoFromNewClient_ReturnsUnauthorizedDespiteAnotherClientsLogin()
    {
        // arrange
        await RegisterAsync();
        await LoginAsync();

        using HttpClient anonymousClient = _factory.CreateClient(ClientOptions());

        // act
        using HttpResponseMessage response = await anonymousClient.GetAsync("/api/auth/manage/info");

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task InfoWithInvalidCookie_ReturnsUnauthorized()
    {
        // arrange
        await RegisterAsync();
        await LoginAsync();

        using HttpClient invalidCookieClient = _factory.CreateClient(ClientOptions(handleCookies: false));

        invalidCookieClient.DefaultRequestHeaders
            .Add("Cookie", ".AspNetCore.Identity.Application=invalid-cookie-value");

        // act
        using HttpResponseMessage response = await invalidCookieClient.GetAsync("/api/auth/manage/info");

        // assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private async Task RegisterAsync()
    {
        using HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/auth/register", new { email = _email, password = Password });

        response.EnsureSuccessStatusCode();
    }

    private async Task LoginAsync()
    {
        using HttpResponseMessage response = await _client
            .PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = _email, password = Password });

        response.EnsureSuccessStatusCode();
    }

    private static WebApplicationFactoryClientOptions ClientOptions(bool handleCookies = true) => new()
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = handleCookies
    };

    // Kept in this file so no other project files need to change.
}