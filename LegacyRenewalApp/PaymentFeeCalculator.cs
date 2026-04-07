using System;

namespace LegacyRenewalApp
{
    public class PaymentFeeCalculator : IPaymentFeeCalculator
    {
        public decimal Calculate(string normalizedPaymentMethod, decimal feeBase, out string note)
        {
            note = string.Empty;

            if (normalizedPaymentMethod == "CARD")
            {
                note = "card payment fee; ";
                return feeBase * 0.02m;
            }

            if (normalizedPaymentMethod == "BANK_TRANSFER")
            {
                note = "bank transfer fee; ";
                return feeBase * 0.01m;
            }

            if (normalizedPaymentMethod == "PAYPAL")
            {
                note = "paypal fee; ";
                return feeBase * 0.035m;
            }

            if (normalizedPaymentMethod == "INVOICE")
            {
                note = "invoice payment; ";
                return 0m;
            }

            throw new ArgumentException("Unsupported payment method");
        }
    }
}