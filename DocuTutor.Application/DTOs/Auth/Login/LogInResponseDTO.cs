using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Application.DTOs.Auth.Login
{
    public class LogInResponseDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; } = false;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;

    }
}
