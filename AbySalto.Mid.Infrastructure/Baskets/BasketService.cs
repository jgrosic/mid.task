using AbySalto.Mid.Application.Baskets;
using AbySalto.Mid.Application.Products;
using AbySalto.Mid.Domain.Entities;
using AbySalto.Mid.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AbySalto.Mid.Infrastructure.Baskets;

public sealed class BasketService(
    AppDbContext db,
    IProductCatalog productCatalog) : IBasketService
{
    public async Task<BasketDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var basket = await db.Basket
            .AsNoTracking()
            .Include(basket => basket.Items)
            .SingleOrDefaultAsync(basket => basket.UserId == userId, cancellationToken);

        if (basket is null)
            return new BasketDto([]);

        var items = basket.Items
            .OrderBy(item => item.ProductId)
            .Select(item => new BasketItemDto(
                item.ProductId, item.ProductTitle, item.UnitPrice, item.Quantity))
            .ToArray();

        return new BasketDto(items);
    }

    public async Task<AddBasketItemResult> AddAsync(
        Guid userId,
        int productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        if (quantity is < 1 or > Basket.MaximumQuantity)
            return AddBasketItemResult.QuantityLimitExceeded;

        var basket = await db.Basket
            .Include(basket => basket.Items)
            .SingleOrDefaultAsync(basket => basket.UserId == userId, cancellationToken);

        var existingItem = basket?.Items.SingleOrDefault(item => item.ProductId == productId);

        if (existingItem is not null)
        {
            if (quantity > Basket.MaximumQuantity - existingItem.Quantity)
                return AddBasketItemResult.QuantityLimitExceeded;

            // Re-adding uses the original snapshot, without another DummyJSON call.
            basket!.AddItem(productId, existingItem.ProductTitle, existingItem.UnitPrice, quantity);
        }
        else
        {
            var product = await productCatalog.GetProductAsync(productId, cancellationToken);

            if (product is null)
                return AddBasketItemResult.ProductNotFound;

            if (basket is null)
            {
                basket = new Basket(userId);
                db.Basket.Add(basket);
            }

            basket.AddItem(product.Id, product.Title, product.Price, quantity);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            /*
                2601: Cannot insert duplicate key row in object with unique index.
                https://learn.microsoft.com/en-us/sql//relational-databases/replication/mssql-eng002601?view=sql-server-ver17

                2627: Violation of constraint . Cannot insert duplicate key in object .
                https://learn.microsoft.com/en-us/sql//relational-databases/replication/mssql-eng002627?view=sql-server-ver17
            */
        {
            // Concurrent creation of the same basket or item is not a successful add.
            return AddBasketItemResult.Conflict;
        }

        return AddBasketItemResult.Added;
    }

    public async Task<bool> RemoveAsync(
        Guid userId,
        int productId,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        var basket = await db.Basket
            .Include(basket => basket.Items)
            .SingleOrDefaultAsync(basket => basket.UserId == userId, cancellationToken);

        if (basket is null || !basket.Items.Any(item => item.ProductId == productId))
            return false;

        // EF Core deletes the orphaned item from this required relationship.
        basket.RemoveItem(productId);
        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}