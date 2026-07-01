using System.Threading.Tasks;
using Stripe;

namespace DocuTutor.Application.Interfaces.Payments
{
    public interface IStripeService
    {
        Task<Customer> GetOrCreateCustomerAsync(string userId, string email, string? existingCustomerId);
        Task<Stripe.Checkout.Session> CreateCheckoutSessionAsync(string customerId, string priceId, string successUrl, string cancelUrl);
        Task<Stripe.BillingPortal.Session> CreatePortalSessionAsync(string customerId, string returnUrl);
        Event ConstructWebhookEvent(string json, string signatureHeader);
    }
}
