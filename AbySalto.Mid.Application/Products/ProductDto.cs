using System.Text.Json.Serialization;

namespace AbySalto.Mid.Application.Products;

public sealed record ProductDto(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Title,
    [property: JsonRequired] decimal Price);
