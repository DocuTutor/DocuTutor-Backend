using System;
using DocuTutor.Domain.Enums;

namespace DocuTutor.Application.DTOs.Subscription
{
    public class SubscriptionDto
    {
        public PlanTier Plan { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime? CurrentPeriodEnd { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
    }
}
