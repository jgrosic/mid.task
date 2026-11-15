namespace AbySalto.Mid.Domain.Entities
{
    public sealed class Basket
    {
        public const int MaximumQuantity = 100;

        private readonly List<BasketItem> _items = [];

        private Basket()
        {
        }

        public Basket(Guid userId)
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("A basket requires an owner", nameof(userId));

            Id = Guid.NewGuid();
            UserId = userId;
        }

        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public IReadOnlyCollection<BasketItem> Items => _items.AsReadOnly();

        public void AddItem(
            int productId,
            string title,
            decimal unitPrice,
            int quantity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productId);
            ArgumentOutOfRangeException.ThrowIfNullOrWhiteSpace(title);
            ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

            if (quantity is < 1 or > MaximumQuantity)
                throw new ArgumentOutOfRangeException(nameof(quantity));

            if (_items.SingleOrDefault(x => x.ProductId == productId) is { } existingItem)
                existingItem.IncreaseQuantity(quantity);
            else
                _items.Add(new BasketItem(Id, productId, title, unitPrice, quantity));
        }


        public void RemoveItem(int productId)
        {
            if (_items.SingleOrDefault(x => x.ProductId == productId) is not { } existing)
                return;

            _items.Remove(existing);
        }
    }
}