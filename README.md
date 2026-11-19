# AbySalto .NET Mid Task

## Usage

After completing the build and database setup below, run the API from the repository root:

```powershell
dotnet run --project AbySalto.Mid --launch-profile https
```

Swagger: https://localhost:7221/

Alternatively, use the `http` launch profile: http://localhost:5269/.

Use [AbySalto.Mid.http](AbySalto.Mid/AbySalto.Mid.http) to quickly check authentication, products, and favorites.

### Authentication

Authentication uses ASP.NET Core Identity with cookies. Execute these requests in order, using a client that stores and sends cookies:

- [x] Register a user: `POST /api/auth/register`.
- [x] Log in with cookies: `POST /api/auth/login?useCookies=true`.
- [x] Get current user information: `GET /api/auth/manage/info`.

Registration and login accept a JSON body:

```json
{
  "email": "test@example.com",
  "password": "Password123!"
}
```

Registration does not log the user in. The info endpoint requires the authentication cookie returned by login.

Visual Studio's `.http` client keeps its own cookie jar. Use a separate client without cookies when checking unauthenticated requests.

### Products

Product GET endpoints are public and do not require authentication. Products are fetched from DummyJSON and are not stored locally.

- [x] Get all products: `GET /api/products`.
- [x] Get an individual product: `GET /api/products/{id}`.

Responses:

- `200 OK`: product list or individual product.
- `400 Bad Request`: product ID is zero or negative.
- `404 Not Found`: the product does not exist.
- `502 Bad Gateway`: an upstream HTTP failure or invalid JSON response.

Errors use ASP.NET Core ProblemDetails responses.

Example:

```http
GET https://localhost:7221/api/products/42
Accept: application/json
```

### Favorites

- [x] Add a product to the current user's favorites: `POST /api/products/{id}/favorite`.

Log in first using cookie authentication.

Responses:

- `204 No Content`: the favorite was added or already exists.
- `400 Bad Request`: product ID is zero or negative.
- `401 Unauthorized`: authentication is required.
- `404 Not Found`: the product does not exist.
- `502 Bad Gateway`: an upstream HTTP failure or invalid JSON response.

Example, after login:

```http
POST https://localhost:7221/api/products/42/favorite
Accept: application/json
```

Favorite listing and removal are not implemented.


### Feature status

Unchecked items are not yet available through the application API.

- [x] User registration, login, and current user information.
- [x] SQL Server persistence with EF Core migrations and Identity stores.
- [x] DummyJSON client for fetching all products and individual products.
- [x] Product list and individual product API endpoints.
- [x] Add products to favorites.
- [x] Basket domain rules with unit tests.
- [ ] Add products to the current user's basket through the API.
- [ ] Remove products from the current user's basket through the API.
- [ ] Get the current user's basket through the API.
- [ ] Product pagination and sorting (bonus).
- [ ] Product caching (bonus).

## Build

### Requirements and configuration

Requires a .NET SDK supporting `net9.0`, .NET/ASP.NET Core 9 runtime, and SQL Server. Product requests also require access to https://dummyjson.com/.

Connection strings:

- API: `AbySalto.Mid/appsettings.Development.json` — database `AbySalto1`.
- Tests: `AbySalto.Mid.Tests/appsettings.json` — database `AbySalto1_Tests`.

User secrets can override the connection strings. Keep the test database separate from the development database.

Run from the repository root:

```powershell
dotnet restore
dotnet tool restore
dotnet build

dotnet ef database update --project AbySalto.Mid.Infrastructure --startup-project AbySalto.Mid -- --environment Development
```

The local EF Core tool is pinned to version `9.0.23`.

### Tests

Tests use TUnit with Microsoft Testing Platform:

```powershell
dotnet run --project AbySalto.Mid.Tests
```

- [x] `Unit`: isolated business-rule tests without a database or external services.
- [x] `Database`: service tests using real SQL Server; the original registration test is now skipped in favor of API tests.
- [x] `Api`: authentication endpoint tests using `WebApplicationFactory` and the test SQL Server database.
- [x] `Integration.Exploratory`: exploratory calls to the real DummyJSON API, marked `[Explicit]`.
- [ ] Product client integration tests.
- [ ] Product endpoint API tests.
- [ ] Favorite persistence and API tests.

Explicit tests only run when selected directly. For example:

```powershell
dotnet run --project AbySalto.Mid.Tests -- --treenode-filter "/*/AbySalto.Mid.Tests.Integration.Exploratory/DummyJson/*"
```

See [helper.ps1](helper.ps1) for additional commands.

## Architecture

- [x] Separate Domain, Application, Infrastructure, and Web API projects.

| Project | Responsibility |
| --- | --- |
| `AbySalto.Mid` | This is the composition root. Web API controllers, Identity endpoints |
| `AbySalto.Mid.Domain` | Entities and business rules. |
| `AbySalto.Mid.Application` | Application contracts and DTOs. |
| `AbySalto.Mid.Infrastructure` | SQL Server persistence, Identity, and the DummyJSON client. |
| `AbySalto.Mid.Tests` | Unit, database, API, and external integration tests. |

Project dependencies (`->` means "references"):

```text
Application    -> Domain
Infrastructure -> Application + Domain
Web API        -> Application + Infrastructure
```

Own API endpoints use MVC controllers. Authentication uses the built-in `MapIdentityApi` endpoints.


### Visual Studio tip

To quickly add a project reference, drag and drop the source project onto the target project in Solution Explorer.
