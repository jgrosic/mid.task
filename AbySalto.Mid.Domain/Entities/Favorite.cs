namespace AbySalto.Mid.Domain.Entities;

public sealed class Favorite
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public int ProductId { get; init; }
}
