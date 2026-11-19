namespace AbySalto.Mid.Application.Favorites;

public interface IFavoriteService
{
    /// <summary>
    /// Adds user's favorite product.
    /// <para>
    /// Adding an existing favorite succeeds without creating a duplicate.
    /// </para>
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="productId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if favorite product is added. False if product do not exist.</returns>
    Task<bool> AddAsync(
        Guid userId,
        int productId,
        CancellationToken cancellationToken
    );
}