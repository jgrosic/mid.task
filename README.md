# Abysalto .net mid task

see: helper.ps1 for helpful commands

Project dependencies, arrow meaning "project is referencing another project"
- Application     -> Domain
- Infrastructure  -> Application + Domain
- WebApi          -> Application + Infrastructure

Projects
AbySalto.Mid.Domain
AbySalto.Mid.Application
AbySalto.Mid.Infrastructure
AbySalto.Mid — Web API


## Setup

Requires a .NET SDK supporting `net9.0`, .NET/ASP.NET Core 9 runtime, and SQL Server.

Connection strings:
- API:      `AbySalto.Mid/appsettings.Development.json` — `AbySalto1`.
- Tests:    `AbySalto.Mid.Tests/appsettings.json`       — `AbySalto1_Tests`; user secrets can override it.

Run from the repository root:

```powershell
dotnet restore
dotnet tool restore
dotnet ef database update --project AbySalto.Mid.Infrastructure --startup-project AbySalto.Mid -- --environment Development
dotnet run --project AbySalto.Mid --launch-profile http
```

Swagger: http://localhost:5269/

## Tests

TUnit with Microsoft Testing Platform. Run: `dotnet test`.

- `Unit`: isolated business-rule tests.
- `Database`: service tests using real SQL Server.
- `Integration`: HTTP/API tests (planned).

## Implemented

User registration via `POST /api/auth/register`, using ASP.NET Core Identity for validation, password hashing, and persistence.


# Tips
## Visual Studio

To quickly add a project reference, drag and drop the source project onto the target project in Solution Explorer.