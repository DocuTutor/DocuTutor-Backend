using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Application.DTOs.Auth.RefreshToken
{
    public class RefreshTokenResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
   
    }
}
