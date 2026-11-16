using AbySalto.Mid.Application.Auth;
using AbySalto.Mid.Infrastructure;
using AbySalto.Mid.Infrastructure.Identity;
using AbySalto.Mid.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AbySalto.Mid.Tests.Database;

internal class RegistrationTests
{
    [Test]
    public async Task Registration_SavesUserAndRejectsDuplicate()
    {
        // arrange

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<RegistrationTests>(optional: true)
            .Build();

        var services = new ServiceCollection();

        services.AddLogging(loggingBuilder => loggingBuilder.AddDebug());
        services.AddInfrastructure(config);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();

        var email = $"{Guid.NewGuid():N}@abysalto.com";
        const string password = "!Password123!";

        var request = new Application.Auth.RegisterRequest(email, password);

        try
        {
            // act

            var registration = await auth.RegisterAsync(request, CancellationToken.None);

            var storedUser = await userManager.FindByEmailAsync(email);

            var passwordIsValid = storedUser is not null && await userManager.CheckPasswordAsync(storedUser, password);

            var duplicate = await auth.RegisterAsync(request, CancellationToken.None);

            // assert

            await Assert.That(registration.Succeeded).IsTrue();
            await Assert.That(registration.User).IsNotNull();

            await Assert.That(storedUser).IsNotNull();
            await Assert.That(storedUser!.Id).IsEqualTo(registration.User!.Id);
            await Assert.That(storedUser.PasswordHash).IsNotEqualTo(password);

            await Assert.That(passwordIsValid).IsTrue();
            await Assert.That(duplicate.Succeeded).IsFalse();
            await Assert.That(duplicate.Errors.Length).IsGreaterThan(0);
        }
        finally
        {
            await db.Users
                .Where(user => user.Email == email)
                .ExecuteDeleteAsync();
        }
    }
}