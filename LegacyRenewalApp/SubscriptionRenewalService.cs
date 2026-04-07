using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly IRenewalRequestValidator _validator;
        private readonly IDiscountCalculator _discountCalculator;
        private readonly IPaymentFeeCalculator _paymentFeeCalculator;
        private readonly ITaxRateProvider _taxRateProvider;
        private readonly IBillingGateway _billingGateway;
        private readonly IInvoiceEmailService _invoiceEmailService;

        public SubscriptionRenewalService()
            : this(
                new RenewalRequestValidator(),
                new DiscountCalculator(),
                new PaymentFeeCalculator(),
                new TaxRateProvider(),
                new LegacyBillingGatewayAdapter(),
                new InvoiceEmailService(new LegacyBillingGatewayAdapter()))
        {
        }

        public SubscriptionRenewalService(
            IRenewalRequestValidator validator,
            IDiscountCalculator discountCalculator,
            IPaymentFeeCalculator paymentFeeCalculator,
            ITaxRateProvider taxRateProvider,
            IBillingGateway billingGateway,
            IInvoiceEmailService invoiceEmailService)
        {
            _validator = validator;
            _discountCalculator = discountCalculator;
            _paymentFeeCalculator = paymentFeeCalculator;
            _taxRateProvider = taxRateProvider;
            _billingGateway = billingGateway;
            _invoiceEmailService = invoiceEmailService;
        }

        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            _validator.Validate(customerId, planCode, seatCount, paymentMethod);

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customerRepository = new CustomerRepository();
            var planRepository = new SubscriptionPlanRepository();

            var customer = customerRepository.GetById(customerId);
            var plan = planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;

            decimal discountAmount = _discountCalculator.Calculate(
                customer,
                plan,
                baseAmount,
                seatCount,
                useLoyaltyPoints,
                out string discountNotes);

            decimal subtotalAfterDiscount = baseAmount - discountAmount;
            string notes = discountNotes;

            if (subtotalAfterDiscount < 300m)
            {
                subtotalAfterDiscount = 300m;
                notes += "minimum discounted subtotal applied; ";
            }

            decimal supportFee = CalculateSupportFee(includePremiumSupport, normalizedPlanCode);
            if (includePremiumSupport)
            {
                notes += "premium support included; ";
            }

            decimal paymentFee = _paymentFeeCalculator.Calculate(
                normalizedPaymentMethod,
                subtotalAfterDiscount + supportFee,
                out string paymentFeeNote);

            notes += paymentFeeNote;

            decimal taxRate = _taxRateProvider.GetTaxRate(customer.Country);
            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            _billingGateway.SaveInvoice(invoice);
            _invoiceEmailService.Send(customer, normalizedPlanCode, invoice);

            return invoice;
        }

        private static decimal CalculateSupportFee(bool includePremiumSupport, string normalizedPlanCode)
        {
            if (!includePremiumSupport)
            {
                return 0m;
            }

            if (normalizedPlanCode == "START")
            {
                return 250m;
            }

            if (normalizedPlanCode == "PRO")
            {
                return 400m;
            }

            if (normalizedPlanCode == "ENTERPRISE")
            {
                return 700m;
            }

            return 0m;
        }
    }
}