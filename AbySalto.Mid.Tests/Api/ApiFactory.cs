using AbySalto.Mid.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AbySalto.Mid.Tests.Api;

/// <summary>
/// Hosts the API in-process using the configured SQL Server test database.
/// Initializes the database once and allows clients to use isolated cookie jars.
/// <para>
/// See <see href="https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-9.0#basic-tests-with-the-default-webapplicationfactory">WebApplicationFactory documentation</see>.
/// </para>
/// </summary>
internal sealed class ApiFactory : WebApplicationFactory<AbySalto.Mid.Program>
{
    public static Task InitializeDatabaseAsync() => DatabaseInitialization.Value;

    private static readonly Lazy<Task> DatabaseInitialization = new(InitializeDatabaseCoreAsync);

    private static async Task InitializeDatabaseCoreAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }


    public static AppDbContext CreateDbContext()
    {
        var connectionString = ConnectionString();

        DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();

        optionsBuilder.UseSqlServer(connectionString);

        DbContextOptions<AppDbContext> options = optionsBuilder.Options;

        return new AppDbContext(options);
    }

    private static readonly IConfigurationRoot TestConfiguration =
        new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<AuthTests>(optional: true)
            .Build();


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddConfiguration(TestConfiguration));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Program reads the connection string before builder.Build(),
        // so supply it through host configuration
        // before the application entry point runs.
        builder.ConfigureHostConfiguration(configuration =>
            configuration.AddConfiguration(TestConfiguration));

        return base.CreateHost(builder);
    }

    private static string ConnectionString()
    {
        var connectionString = TestConfiguration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException(
                                   "The test database connection string is required.");

        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;

        return databaseName.EndsWith("Tests", StringComparison.OrdinalIgnoreCase)
            ? connectionString
            : throw new InvalidOperationException("For safety, the API test database name must end with 'Tests'.");
    }
}