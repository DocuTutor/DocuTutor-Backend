using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Application.DTOs.Auth.Register
{
    public class RegisterRequestDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
