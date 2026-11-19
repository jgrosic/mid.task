using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using AbySalto.Mid.Domain.Entities;

namespace AbySalto.Mid.Application.Baskets;

public sealed record AddBasketItemRequest(
    [property: JsonRequired]
    [Range(1, Basket.MaximumQuantity)] int Quantity);
