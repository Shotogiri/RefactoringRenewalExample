namespace LegacyRenewalApp
{
    public interface IDiscountCalculator
    {
        decimal Calculate(
            Customer customer,
            SubscriptionPlan plan,
            decimal baseAmount,
            int seatCount,
            bool useLoyaltyPoints,
            out string notes);
    }
}