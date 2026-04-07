namespace LegacyRenewalApp
{
    public interface IPaymentFeeCalculator
    {
        decimal Calculate(string normalizedPaymentMethod, decimal feeBase, out string note);
    }
}