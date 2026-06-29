using MimeKit;

using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Infrastructure.ExternalServices.EmailService
{
    public class EmailMessage
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }

        public EmailMessage(IEnumerable<string> to, string subject, string content)
        {
            To = new List<MailboxAddress>();


            foreach (var email in to)
            {
                if (MailboxAddress.TryParse(email, out var address))
                {
                    To.Add(address);
                }
            }
            Subject = subject;
            Content = content;
        }
    }
}
