using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Application.DTOs.Subscription
{
    public class PlanDto
    {
        public string Tier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Interval { get; set; } = "month";
        public bool Highlight { get; set; }
        public List<string> Features { get; set; } = new();
    }
}