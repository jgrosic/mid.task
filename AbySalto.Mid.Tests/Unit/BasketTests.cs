using AbySalto.Mid.Domain.Entities;

namespace AbySalto.Mid.Tests.Unit;

public class BasketTests
{
    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(101)]
    public async Task InvalidQuantity_IsRejected(int quantity)
    {
        // arrange
        var basket = new Basket(Guid.NewGuid());

        // act
        var act = ()=> basket.AddItem(productId: 1, title: "Product", unitPrice: 0, quantity: quantity);

        // assert
        await Assert.That(act).Throws<ArgumentOutOfRangeException>();
        await Assert.That(basket.Items.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Remove_DeletesWholeItem()
    {
        // arrange
        var basket = new Basket(Guid.NewGuid());

        basket.AddItem(productId: 1, title: "Product", unitPrice: 0, quantity: 5);
        // act
        basket.RemoveItem(1);

        // assert
        await Assert.That(basket.Items.Count).IsEqualTo(0);
    }


}