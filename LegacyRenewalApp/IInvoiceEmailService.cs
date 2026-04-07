namespace LegacyRenewalApp
{
    public interface IInvoiceEmailService
    {
        void Send(Customer customer, string normalizedPlanCode, RenewalInvoice invoice);
    }
}