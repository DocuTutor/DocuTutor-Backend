using DocuTutor.Domain.Entities;
using DocuTutor.Infrastructure.ExternalServices.EmailService;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Infrastructure.ExternalInterfaces.IEmailInterface
{
    public interface IEmailSender
    {
        Task SendEmailAsync(EmailMessage message);
    }
}
