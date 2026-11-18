using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using AbySalto.Mid.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AbySalto.Mid.Controllers;

[ApiController]
[Route("api/products")]
[AllowAnonymous]
[Produces(MediaTypeNames.Application.Json)]
public sealed class ProductController(IProductCatalog catalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAllProducts(CancellationToken cancellationToken)
    {
        var result = await catalog.GetProductsAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProductById
    (
        [Range(1, int.MaxValue)] int id
        , CancellationToken cancellationToken)
    {
        var result = await catalog.GetProductAsync(id, cancellationToken);

        return result is null
            ? NotFound()
            : Ok(result);
    }
}