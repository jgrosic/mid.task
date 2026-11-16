using AbySalto.Mid.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace AbySalto.Mid.Infrastructure.Identity;

public sealed class IdentityAuthService(UserManager<ApplicationUser> userManager)
    : IAuthService
{
    public async Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var email = request.Email.Trim();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(x => x.Description).ToArray();

            return new RegistrationResult(null, errors);
        }

        return new RegistrationResult(new UserDto(user.Id, user.Email!), []);
    }
}