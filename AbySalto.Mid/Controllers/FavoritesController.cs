using System.ComponentModel.DataAnnotations;
using AbySalto.Mid.Application.Favorites;
using AbySalto.Mid.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AbySalto.Mid.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class FavoritesController(
    IFavoriteService favorites,
    UserManager<ApplicationUser> userManager,
    ILogger<FavoritesController> logger
) : ControllerBase
{
    [HttpPost("{id:int}/favorite")]
    public async Task<IActionResult> Add(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var added = await favorites.AddAsync(
            user.Id, id, cancellationToken);

        if (added)
            logger.LogInformation(
                "Product {ProductId} is a favorite for user {UserId}.",
                id, user.Id);

        return added ? NoContent() : NotFound();
    }
}