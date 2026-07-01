using DocuTutor.Domain.Enums;

namespace DocuTutor.Application.DTOs.Subscription
{
    public class CreateCheckoutSessionRequestDto
    {
        public PlanTier Plan { get; set; }
    }
}