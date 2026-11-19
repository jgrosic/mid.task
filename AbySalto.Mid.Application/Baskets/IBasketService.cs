namespace AbySalto.Mid.Application.Baskets;

public interface IBasketService
{
    Task<BasketDto> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task<AddBasketItemResult> AddAsync(
        Guid userId,
        int productId,
        int quantity,
        CancellationToken cancellationToken);

    // Returns whether an item was removed. A missing item is a successful no-op.
    Task<bool> RemoveAsync(Guid userId, int productId, CancellationToken cancellationToken);
}

public enum AddBasketItemResult
{
    Added,
    ProductNotFound,
    QuantityLimitExceeded,
    Conflict
}
