namespace AbySalto.Mid.Application.Products;

public interface IProductCatalog
{
    Task<IReadOnlyList<ProductDto>> GetProductsAsync(CancellationToken cancellationToken);

    Task<ProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken);
}