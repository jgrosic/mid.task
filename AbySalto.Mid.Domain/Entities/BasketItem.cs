namespace AbySalto.Mid.Domain.Entities;

public class BasketItem
{

    private BasketItem()
    {
    }

    internal BasketItem(
        Guid basketId,
        int productId,
        string title,
        decimal unitPrice,
        int quantity)
    {

        Id = Guid.NewGuid();
        BasketId = basketId;
        ProductId = productId;
        ProductTitle = title;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }
    public Guid BasketId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductTitle { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    internal void IncreaseQuantity(int quantity)
    {
        var availableQuantity = Basket.MaximumQuantity - Quantity;

        if (quantity < 1 || quantity > availableQuantity)
            throw new ArgumentOutOfRangeException(nameof(quantity), $"Total quantity cannot exceed {Basket.MaximumQuantity}.");

        Quantity += quantity;
    }

}