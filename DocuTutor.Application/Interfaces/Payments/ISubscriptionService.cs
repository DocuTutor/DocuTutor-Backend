using System.Collections.Generic;
using System.Threading.Tasks;
using DocuTutor.Application.DTOs.Subscription;
using DocuTutor.Application.Response;
using Stripe;
using PlanTier = DocuTutor.Domain.Enums.PlanTier;

namespace DocuTutor.Application.Interfaces.Payments
{
    public interface ISubscriptionService
    {
        List<PlanDto> GetPlans();
        Task<Response<SubscriptionDto>> GetMySubscriptionAsync(string userId);
        Task<Response<CheckoutSessionResponseDto>> CreateCheckoutSessionAsync(string userId, PlanTier plan);
        Task<Response<PortalSessionResponseDto>> CreatePortalSessionAsync(string userId);
        Task HandleWebhookEventAsync(Event stripeEvent);
    }
}
