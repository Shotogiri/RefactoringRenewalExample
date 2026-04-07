namespace LegacyRenewalApp
{
    public interface ITaxRateProvider
    {
        decimal GetTaxRate(string country);
    }
}