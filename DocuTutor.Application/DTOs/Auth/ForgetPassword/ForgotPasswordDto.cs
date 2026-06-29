using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DocuTutor.Application.DTOs.Auth.ForgetPassword
{
      public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [Url]
        public string? ClientUri { get; set; }
    }
}
