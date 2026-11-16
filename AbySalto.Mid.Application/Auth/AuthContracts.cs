using System.ComponentModel.DataAnnotations;

namespace AbySalto.Mid.Application.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(256)]
    string Email,
    [Required, StringLength(128, MinimumLength = 8)]
    string Password
);

public sealed record UserDto(Guid Id, string Email);

public sealed record RegistrationResult(UserDto? User, string[] Errors)
{
    public bool Succeeded => User is not null;
};

public interface IAuthService
{
    Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    );
}