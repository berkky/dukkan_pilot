namespace DukkanPilot.Web.Areas.Business.Models;

public static class OrderLoyaltyHelper
{
    public static string BuildCompletionDescription(string orderNumber)
    {
        return $"Sipariş tamamlandı: {orderNumber}";
    }

    public static int CalculateEarnedPoints(decimal totalAmount, decimal pointsPerAmount)
    {
        if (pointsPerAmount <= 0)
        {
            return 0;
        }

        return (int)Math.Floor(totalAmount / pointsPerAmount);
    }
}
