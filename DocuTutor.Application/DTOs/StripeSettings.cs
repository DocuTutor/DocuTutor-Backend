using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Application.DTOs
{
    public class StripeSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Prices { get; set; } = new();
    }
}