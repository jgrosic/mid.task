using System.ComponentModel.DataAnnotations;
using AbySalto.Mid.Application.Baskets;
using AbySalto.Mid.Domain.Entities;
using AbySalto.Mid.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AbySalto.Mid.Controllers;

[ApiController]
[Authorize]
[Route("api/basket")]
public sealed class BasketController(
    IBasketService baskets,
    UserManager<ApplicationUser> userManager,
    ILogger<BasketController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BasketDto>> Get(CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var basket = await baskets.GetAsync(user.Id, cancellationToken);
        return Ok(basket);
    }

    [HttpPost("items/{id:int}")]
    public async Task<IActionResult> Add(
        [Range(1, int.MaxValue)] int id,
        AddBasketItemRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var result = await baskets.AddAsync(user.Id, id, request.Quantity, cancellationToken);

        switch (result)
        {
            case AddBasketItemResult.Added:
                logger.LogInformation(
                    "Added {Quantity} of product {ProductId} to the basket for user {UserId}.",
                    request.Quantity, id, user.Id);
                return NoContent();

            case AddBasketItemResult.ProductNotFound:
                logger.LogInformation("Product cannot be found - not added to the basket.");
                return NotFound();

            case AddBasketItemResult.QuantityLimitExceeded:
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid basket quantity",
                    detail: $"Total quantity per product cannot exceed {Basket.MaximumQuantity}.");

            case AddBasketItemResult.Conflict:
                logger.LogWarning(
                    "Concurrent basket creation or item addition for user {UserId} and product {ProductId}.",
                    user.Id, id);
                return Problem(
                    statusCode: StatusCodes.Status409Conflict,
                    title: "Basket changed concurrently",
                    detail: "Another request added the basket or item. Reload the basket before trying again.");

            default:
                throw new InvalidOperationException("Unexpected basket add result.");
        }
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> Remove(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null)
            return Unauthorized();

        var removed = await baskets.RemoveAsync(user.Id, id, cancellationToken);

        if (removed)
            logger.LogInformation(
                "Removed product {ProductId} from the basket for user {UserId}.", id, user.Id);

        return NoContent();
    }
}
