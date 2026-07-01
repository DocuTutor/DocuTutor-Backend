using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using DocuTutor.Application.DTOs;
using DocuTutor.Application.DTOs.Subscription;
using DocuTutor.Application.Interfaces.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuTutor.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController(
        ISubscriptionService subscriptionService,
        IStripeService stripeService,
        ILogger<SubscriptionController> logger
    ) : ControllerBase
    {
        [HttpGet("plans")]
        [AllowAnonymous]
        public IActionResult GetPlans() => Ok(subscriptionService.GetPlans());

        [HttpGet("my-subscription")]
        [Authorize]
        public async Task<IActionResult> GetMySubscription()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await subscriptionService.GetMySubscriptionAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession(
            CreateCheckoutSessionRequestDto request
        )
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await subscriptionService.CreateCheckoutSessionAsync(userId, request.Plan);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("portal-session")]
        [Authorize]
        public async Task<IActionResult> CreatePortalSession()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
            var result = await subscriptionService.CreatePortalSessionAsync(userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            try
            {
                var stripeEvent = stripeService.ConstructWebhookEvent(json, signature!);
                await subscriptionService.HandleWebhookEventAsync(stripeEvent);
                return Ok();
            }
            catch (Stripe.StripeException ex)
            {
                logger.LogWarning(ex, "Stripe webhook signature verification failed");
                return BadRequest();
            }
        }
    }
}
