namespace AbySalto.Mid.Application.Baskets;

public sealed record BasketDto(IReadOnlyList<BasketItemDto> Items)
{
    public decimal TotalPrice => Items.Sum(item => item.TotalPrice);
}

public sealed record BasketItemDto(
    int ProductId,
    string ProductTitle,
    decimal UnitPrice,
    int Quantity)
{
    public decimal TotalPrice => UnitPrice * Quantity;
}
