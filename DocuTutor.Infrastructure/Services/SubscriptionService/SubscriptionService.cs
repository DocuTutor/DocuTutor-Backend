using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocuTutor.Application.DTOs;
using DocuTutor.Application.DTOs.Subscription;
using DocuTutor.Application.Interfaces.Payments;
using DocuTutor.Application.Response;
using DocuTutor.Domain.Enums;
using DocuTutor.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using PlanTier = DocuTutor.Domain.Enums.PlanTier;
using SubscriptionStatus = DocuTutor.Domain.Enums.SubscriptionStatus;

namespace DocuTutor.Infrastructure.Services.SubscriptionService
{
    public class SubscriptionService(
        DocuTutorDbContext db,
        IStripeService stripeService,
        IOptions<StripeSettings> stripeOptions) : Application.Interfaces.Payments.ISubscriptionService
    {
        private readonly StripeSettings _settings = stripeOptions.Value;

        public List<PlanDto> GetPlans() => new()
        {
            new PlanDto
            {
                Tier = "Free",
                Name = "Free",
                Price = 0,
                Highlight = false,
                Features = new List<string> { "3 documents / mo", "Chat & summary", "10 quiz questions" }
            },
            new PlanDto
            {
                Tier = "Pro",
                Name = "Pro",
                Price = 9,
                Highlight = true,
                Features = new List<string> { "Unlimited documents", "Unlimited chat & quizzes", "Priority processing", "Export to Notion / Markdown" }
            },
            new PlanDto
            {
                Tier = "StudentPlus",
                Name = "Student Plus",
                Price = 5,
                Highlight = false,
                Features = new List<string> { "All of Pro", "Verified student badge", "Group study (soon)" }
            },
        };

        public async Task<Response<SubscriptionDto>> GetMySubscriptionAsync(string userId)
        {
            var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
            var dto = new SubscriptionDto
            {
                Plan = sub?.Plan ?? PlanTier.Free,
                Status = sub?.Status ?? SubscriptionStatus.Active,
                CurrentPeriodEnd = sub?.CurrentPeriodEnd,
                CancelAtPeriodEnd = sub?.CancelAtPeriodEnd ?? false
            };
            return Response<SubscriptionDto>.Success(dto);
        }

        public async Task<Response<CheckoutSessionResponseDto>> CreateCheckoutSessionAsync(string userId, PlanTier plan)
        {
            if (plan == PlanTier.Free)
                return Response<CheckoutSessionResponseDto>.Failure("Free plan does not require checkout", 400);

            if (!_settings.Prices.TryGetValue(plan.ToString(), out var priceId) || string.IsNullOrEmpty(priceId))
                return Response<CheckoutSessionResponseDto>.Failure("Unknown plan", 400);

            try
            {
                var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
                var user = await db.Users.FirstAsync(u => u.Id == userId);

                var customer = await stripeService.GetOrCreateCustomerAsync(userId, user.Email!, sub?.StripeCustomerId);

                if (sub == null)
                {
                    sub = new Domain.Entities.Subscription { Id = Guid.NewGuid(), UserId = userId, Plan = PlanTier.Free };
                    db.Subscriptions.Add(sub);
                }
                sub.StripeCustomerId = customer.Id;
                sub.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                var session = await stripeService.CreateCheckoutSessionAsync(
                    customer.Id, priceId, _settings.SuccessUrl, _settings.CancelUrl);

                return Response<CheckoutSessionResponseDto>.Success(new CheckoutSessionResponseDto { CheckoutUrl = session.Url });
            }
            catch (StripeException ex)
            {
                return Response<CheckoutSessionResponseDto>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}", 502);
            }
        }

        public async Task<Response<PortalSessionResponseDto>> CreatePortalSessionAsync(string userId)
        {
            var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (sub?.StripeCustomerId == null)
                return Response<PortalSessionResponseDto>.Failure("No billing account yet", 400);

            try
            {
                var portal = await stripeService.CreatePortalSessionAsync(sub.StripeCustomerId, _settings.CancelUrl);
                return Response<PortalSessionResponseDto>.Success(new PortalSessionResponseDto { PortalUrl = portal.Url });
            }
            catch (StripeException ex)
            {
                return Response<PortalSessionResponseDto>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}", 502);
            }
        }

        public async Task HandleWebhookEventAsync(Event stripeEvent)
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                {
                    var session = (Stripe.Checkout.Session)stripeEvent.Data.Object;
                    var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.StripeCustomerId == session.CustomerId);
                    if (sub != null)
                    {
                        sub.StripeSubscriptionId = session.SubscriptionId;
                        sub.Status = SubscriptionStatus.Active;
                        sub.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
                    break;
                }
                case "customer.subscription.created":
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                {
                    var stripeSub = (Stripe.Subscription)stripeEvent.Data.Object;
                    var sub = await db.Subscriptions.FirstOrDefaultAsync(s =>
                        s.StripeSubscriptionId == stripeSub.Id || s.StripeCustomerId == stripeSub.CustomerId);
                    if (sub != null)
                    {
                        sub.StripeSubscriptionId = stripeSub.Id;
                        sub.Status = MapStatus(stripeSub.Status);
                        sub.CurrentPeriodEnd = stripeSub.Items.Data.FirstOrDefault()?.CurrentPeriodEnd;
                        sub.CancelAtPeriodEnd = stripeSub.CancelAtPeriodEnd;
                        sub.Plan = ResolvePlanFromPriceId(stripeSub.Items.Data.FirstOrDefault()?.Price?.Id);
                        sub.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
                    break;
                }
                case "invoice.payment_failed":
                {
                    var invoice = (Invoice)stripeEvent.Data.Object;
                    var sub = await db.Subscriptions.FirstOrDefaultAsync(s => s.StripeCustomerId == invoice.CustomerId);
                    if (sub != null)
                    {
                        sub.Status = SubscriptionStatus.PastDue;
                        sub.UpdatedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
                    break;
                }
            }
        }

        private PlanTier ResolvePlanFromPriceId(string? priceId)
        {
            if (priceId == null) return PlanTier.Free;
            foreach (var (tier, id) in _settings.Prices)
                if (id == priceId) return Enum.Parse<PlanTier>(tier);
            return PlanTier.Free;
        }

        private static SubscriptionStatus MapStatus(string stripeStatus) => stripeStatus switch
        {
            "active" => SubscriptionStatus.Active,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Canceled,
            "unpaid" => SubscriptionStatus.Unpaid,
            "trialing" => SubscriptionStatus.Trialing,
            _ => SubscriptionStatus.Incomplete
        };
    }
}
