using Gateway.Services;

namespace Gateway.UnitTests;

public class RentalPriceCalculatorTests
{
    [Fact]
    public void Calculate_ThreeDays_ReturnsDaysTimesPrice()
    {
        var price = RentalPriceCalculator.Calculate("2021-10-08", "2021-10-11", 3500);
        Assert.Equal(10500, price);
    }

    [Fact]
    public void Calculate_SameDatesDifferentOrder_ReturnsSamePrice()
    {
        var price = RentalPriceCalculator.Calculate("2021-10-11", "2021-10-08", 3500);
        Assert.Equal(10500, price);
    }

    [Fact]
    public void Calculate_OneDay_ReturnsPricePerDay()
    {
        var price = RentalPriceCalculator.Calculate("2021-10-08", "2021-10-09", 3500);
        Assert.Equal(3500, price);
    }
}
