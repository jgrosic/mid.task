using AbySalto.Mid.Application.Favorites;
using AbySalto.Mid.Application.Products;
using AbySalto.Mid.Domain.Entities;
using AbySalto.Mid.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AbySalto.Mid.Infrastructure.Favorites;

public sealed class FavoritesService(
    AppDbContext db,
    IProductCatalog productCatalog
) : IFavoriteService
{
    public async Task<bool> AddAsync(
        Guid userId,
        int productId,
        CancellationToken cancellationToken
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);

        var exists = await db.Favorite
                .AnyAsync(
                    f => f.UserId == userId
                         && f.ProductId == productId
                    , cancellationToken)
            ;

        if (exists)
            return true;

        var product = await productCatalog.GetProductAsync(productId, cancellationToken);

        if (product is null)
            return false;

        var favorite = new Favorite
        {
            UserId = userId,
            ProductId = productId
        };

        db.Favorite.Add(favorite);

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
            // Another request may have added the same favorite.
            db.Entry(favorite).State = EntityState.Detached;
        }

        return true;
    }
}