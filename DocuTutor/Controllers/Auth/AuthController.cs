using DocuTutor.Application.DTOs.Auth.ForgetPassword;
using DocuTutor.Application.DTOs.Auth.Login;
using DocuTutor.Application.DTOs.Auth.RefreshToken;
using DocuTutor.Application.DTOs.Auth.Register;
using DocuTutor.Application.DTOs.Auth.ResetPassword;
using DocuTutor.Application.Interfaces.Auth;
using DocuTutor.Application.Response;
using DocuTutor.Domain.Entities;
using DocuTutor.Infrastructure.Services.AuthService;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace DocuTutor.Presentation.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService,IConfiguration configuration) : ControllerBase
    {
        //, ILogger<AuthController> logger

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
           
            var result = await authService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequestDto request)
        {
           
            var result = await authService.RefreshTokenAsync(request);
            return StatusCode(result.StatusCode, result);
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await authService.LogoutAsync(userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<Response<LogInResponseDTO>>> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "Validation failed",
                        400,
                        validationErrors));
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "Email is required",
                        400));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(
                    Response<LogInResponseDTO>.Failure(
                        new LogInResponseDTO(),
                        "Password is required",
                        400));
            }

            try
            {
                var result = await authService.LoginAsync(request);
                return StatusCode(result.StatusCode, result);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499,Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"Request was cancelled", 499));
            }
            catch (Exception ex)
            {
                return StatusCode(500,Response<LogInResponseDTO>.Failure(new LogInResponseDTO(),"An unexpected error occurred. Please try again later.",500,[ex.Message]));
            }
        }

        [HttpPost("forgotpassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPassword)
        {
            //if (!ModelState.IsValid)
            //{
            //    var validationErrors = ModelState.Values
            //        .SelectMany(v => v.Errors)
            //        .Select(e => e.ErrorMessage)
            //        .ToList();

            //    return BadRequest(
            //        Response<string>.Failure(
            //            "",
            //            "Validation failed",
            //            400,
            //            validationErrors));
            //}
            var result = await authService.ForgotPasswordAsync(forgotPassword);
            return StatusCode(result.StatusCode, result);


        }

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPassword)
        {
            //if (!ModelState.IsValid)
            //{
            //    var validationErrors = ModelState.Values
            //        .SelectMany(v => v.Errors)
            //        .Select(e => e.ErrorMessage)
            //        .ToList();

            //    return BadRequest(
            //        Response<string>.Failure(
            //            "",
            //            "Validation failed",
            //            400,
            //            validationErrors));
            //}

            var result = await authService.ResetPasswordAsync(resetPassword);
            return StatusCode(result.StatusCode, result);
        }


    }

}


