using Hamlet.Sim.Agent;
using Hamlet.Sim.Economy;
using Xunit;

namespace Hamlet.Sim.Tests;

/// <summary>
/// Shop contract (§7.3): a purchase moves meals and money together, never
/// exceeds stock or wallet, and never mints or destroys a coin.
/// </summary>
public class ShopTests
{
    private static (LedgerBook Ledgers, ShopBook Shops, Shop Shop, Household Household) World()
    {
        var ledgers = new LedgerBook();
        var shops = new ShopBook(ledgers);
        var shop = shops.Found(4, 4, stock: 20, openingCash: 50);
        var household = new Household(id: 1, homeX: 2, homeY: 2, workX: 8, workY: 2, meals: 0)
        {
            Wallet = ledgers.OpenLedger(100),
            CornerShop = shop,
        };
        return (ledgers, shops, shop, household);
    }

    [Fact]
    public void BuyingMealsMovesMoneyAndStockWithoutMinting()
    {
        var (ledgers, _, shop, household) = World();

        Assert.True(household.TryBuyMeals(7));

        Assert.Equal(7, household.MealsStocked);
        Assert.Equal(13, shop.MealsInStock);
        Assert.Equal(100 - 7 * Shop.PricePerMeal, household.Wallet!.Balance);
        Assert.Equal(50 + 7 * Shop.PricePerMeal, shop.Ledger.Balance);
        Assert.Equal(150, ledgers.TotalMoney); // opening 100 + 50, still conserved
    }

    [Fact]
    public void ABrokeHouseholdBuysOnlyWhatItCanAfford()
    {
        var (ledgers, _, shop, household) = World();
        household.Wallet = ledgers.OpenLedger(12);
        var openingMoney = ledgers.TotalMoney;

        Assert.True(household.TryBuyMeals(7));

        Assert.Equal(2, household.MealsStocked); // 12 / 5 = two meals
        Assert.Equal(2, household.Wallet!.Balance);
        Assert.Equal(18, shop.MealsInStock);
        Assert.Equal(openingMoney, ledgers.TotalMoney); // transfers only, never minted
    }

    [Fact]
    public void AnEmptyShopSellsNothing()
    {
        var ledgers = new LedgerBook();
        var shop = new ShopBook(ledgers).Found(4, 4, stock: 0);
        var household = new Household(id: 1, homeX: 2, homeY: 2, workX: 8, workY: 2, meals: 0)
        {
            Wallet = ledgers.OpenLedger(100),
            CornerShop = shop,
        };

        Assert.False(household.TryBuyMeals(7));
        Assert.Equal(0, household.MealsStocked);
        Assert.Equal(100, household.Wallet!.Balance);
        Assert.Equal(0, shop.Ledger.Balance);
    }

    [Fact]
    public void AHouseholdWithoutAWalletCannotShop()
    {
        var (_, _, shop, household) = World();
        household.Wallet = null;

        Assert.False(household.TryBuyMeals(7));
        Assert.Equal(0, household.MealsStocked);
        Assert.Equal(20, shop.MealsInStock);
    }

    [Fact]
    public void NearestShopBreaksTiesById()
    {
        var shops = new ShopBook(new LedgerBook());
        var first = shops.Found(5, 5);
        shops.Found(1, 1);

        Assert.Same(first, shops.Nearest(3, 3)); // both four tiles away: id order decides
    }

    [Fact]
    public void RestockingMovesMoneyToTheSupplierAndRaisesStock()
    {
        var ledgers = new LedgerBook();
        var shops = new ShopBook(ledgers);
        var firms = new FirmBook(ledgers);
        var supplier = firms.Found(6, 6, openingCash: 0);
        var shop = shops.Found(4, 4, stock: 10, openingCash: 600);
        shop.Supplier = supplier;
        var openingMoney = ledgers.TotalMoney;

        Assert.True(shop.TryRestock(Shop.RestockBatch, out var bought));

        Assert.Equal(Shop.RestockBatch, bought);
        Assert.Equal(10 + Shop.RestockBatch, shop.MealsInStock);
        Assert.Equal(600 - Shop.RestockBatch * Shop.WholesalePricePerMeal, shop.Ledger.Balance);
        Assert.Equal(Shop.RestockBatch * Shop.WholesalePricePerMeal, supplier.Ledger.Balance);
        Assert.Equal(openingMoney, ledgers.TotalMoney);
    }

    [Fact]
    public void AnUnfundedShopBuysOnlyWhatItCanAfford()
    {
        var ledgers = new LedgerBook();
        var supplier = new FirmBook(ledgers).Found(6, 6, openingCash: 0);
        var shop = new ShopBook(ledgers).Found(4, 4, stock: 0, openingCash: 7);
        shop.Supplier = supplier;

        Assert.True(shop.TryRestock(Shop.RestockBatch, out var bought));

        Assert.Equal(7 / Shop.WholesalePricePerMeal, bought); // two meals at wholesale
        Assert.Equal(bought, shop.MealsInStock);
    }

    [Fact]
    public void AShopWithNoSupplierCannotRestock()
    {
        var shop = new ShopBook(new LedgerBook()).Found(4, 4, stock: 0, openingCash: 300);

        Assert.False(shop.TryRestock(Shop.RestockBatch, out var bought));
        Assert.Equal(0, bought);
        Assert.Equal(0, shop.MealsInStock);
    }
}
