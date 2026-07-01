using System;
using DocuTutor.Domain.Enums;

namespace DocuTutor.Domain.Entities
{
    public class Subscription
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public PlanTier Plan { get; set; } = PlanTier.Free;
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string? StripePriceId { get; set; }

        public DateTime? CurrentPeriodEnd { get; set; }
        public bool CancelAtPeriodEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
