using DocuTutor.Application.DTOs.Auth.ForgetPassword;
using DocuTutor.Application.DTOs.Auth.Login;
using DocuTutor.Application.DTOs.Auth.RefreshToken;
using DocuTutor.Application.DTOs.Auth.Register;
using DocuTutor.Application.DTOs.Auth.ResetPassword;
using DocuTutor.Application.Response;
using DocuTutor.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<Response<string>> RegisterAsync(RegisterRequestDto request);
        Task<Response<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto refreshTokenRequest);

        Task<Response<LogInResponseDTO>> LoginAsync(LoginRequestDto request);
        Task<Response<string>> LogoutAsync(string userId);

        Task<Response<string>> ForgotPasswordAsync(ForgotPasswordDto forgotPassword);

        Task<Response<string>> ResetPasswordAsync(ResetPasswordDto resetPassword);


    }
}
