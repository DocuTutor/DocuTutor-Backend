using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Domain.Enums
{
    public enum SubscriptionStatus
    {
        Incomplete,
        Trialing,
        Active,
        PastDue,
        Canceled,
        Unpaid
    }
}
