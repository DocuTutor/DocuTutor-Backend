using System.Collections.Generic;
using System.Threading.Tasks;
using DocuTutor.Application.DTOs;
using DocuTutor.Application.Interfaces.Payments;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace DocuTutor.Infrastructure.ExternalServices.Payments
{
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _settings;

        public StripeService(IOptions<StripeSettings> stripeOptions)
        {
            _settings = stripeOptions.Value;
            StripeConfiguration.ApiKey = _settings.SecretKey;
        }

        public async Task<Customer> GetOrCreateCustomerAsync(string userId, string email, string? existingCustomerId)
        {
            var customerService = new CustomerService();

            if (!string.IsNullOrEmpty(existingCustomerId))
                return await customerService.GetAsync(existingCustomerId);

            return await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = email,
                Metadata = new Dictionary<string, string> { { "userId", userId } }
            });
        }

        public async Task<Session> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl)
        {
            var service = new SessionService();
            return await service.CreateAsync(new SessionCreateOptions
            {
                Mode = "subscription",
                Customer = customerId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Price = priceId, Quantity = 1 }
                },
                SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = cancelUrl,
                AllowPromotionCodes = true
            });
        }

        public async Task<Stripe.BillingPortal.Session> CreatePortalSessionAsync(string customerId, string returnUrl)
        {
            var service = new Stripe.BillingPortal.SessionService();
            return await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            });
        }

        public Event ConstructWebhookEvent(string json, string signatureHeader)
            => EventUtility.ConstructEvent(json, signatureHeader, _settings.WebhookSecret);
    }
}
