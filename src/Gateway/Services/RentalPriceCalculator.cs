namespace Gateway.Services;

public static class RentalPriceCalculator
{
    public static int Calculate(string dateFrom, string dateTo, int pricePerDay)
    {
        var from = DateOnly.Parse(dateFrom);
        var to = DateOnly.Parse(dateTo);
        var days = Math.Abs(to.DayNumber - from.DayNumber);
        return days * pricePerDay;
    }
}
