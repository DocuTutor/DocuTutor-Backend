using DocuTutor.Application.Response;
using DocuTutor.Domain.Entities;
using DocuTutor.Infrastructure.ExternalInterfaces.IJwtTokenService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DocuTutor.Application.DTOs.Auth.Register;
using DocuTutor.Application.DTOs.Auth.RefreshToken;
using Microsoft.EntityFrameworkCore;
using DocuTutor.Application.Interfaces.Auth;
using DocuTutor.Application.DTOs.Auth.Login;

namespace DocuTutor.Infrastructure.Services.AuthService
{
    public class AuthService(UserManager<ApplicationUser> userManager, IValidator<RegisterRequestDto> validator, IValidator<RefreshTokenRequestDto> refreshTokenValidator, IJwtTokenService _jwtService, IConfiguration config, IServiceScopeFactory serviceScopeFactory) : IAuthService
    {
        public async Task<Response<string>> RegisterAsync(RegisterRequestDto request)
        {
            try
            {
                //Validation Handeled Here only For Now due To Problem In Auto Validation.

                var validation = await validator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<string>.Failure("", "Validation failed", 400, errors);
                }

                //needs refactoring later
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null && existingUser.IsActive)  //Instead of Reregistering the user, we can REACTIVATE the user later
                {
                    return Response<string>.Failure("",
                        "Email already exists",
                        400,
                        ["A user with this email is already registered."]
                    );
                }
                var user = new ApplicationUser(request.Email, request.Email, request.FullName);


                var result = await userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return Response<string>.Failure("",
                        "Validation failed",
                        400,
                        result.Errors.Select(e => e.Description).ToList()
                    );
                }



                //---------Needed Only When Debugging Refresh Token Endpoint Until Login Endpoint Created------
                var newAccessToken = await _jwtService.GenerateTokenAsync(user);
                var newRefreshToken = await _jwtService.GenerateRefreshToken();
                var refreshTokenDays = config.GetValue<int>("JWT:RefreshTokenDurationInDays");

                user.RefreshTokens.Add(new RefreshToken
                {
                    Token = newRefreshToken,
                    CreatedOn = DateTime.UtcNow,
                    ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenDays)
                });
                var updateResult = await userManager.UpdateAsync(user);

                Console.WriteLine(newRefreshToken);
                Console.WriteLine(newAccessToken);
                ////---------------------------------------


                return Response<string>.Success("Done successfully", "User registered successfully", 201);
            }
            catch (Exception ex)
            {
                return Response<string>.Failure(
                    "",
                    $"An unexpected error occurred.",
                    500,
                    []
                );
            }
        }

        public async Task<Response<RefreshTokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {

            try
            {
                //Validation Handeled Here only For Now due To Problem In Auto Validation.
                var validation = await refreshTokenValidator.ValidateAsync(request);

                if (!validation.IsValid)
                {
                    var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Validation failed", 400, errors);
                }


                ClaimsPrincipal principal;
                try
                {
                    principal = await _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);
                }
                catch (Exception ex)
                {
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Invalid access token", 401);
                }

                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Invalid access token", 401);

                var user = await userManager.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.IsActive)
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "User not found", 404);

                var storedToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token.Trim() == request.RefreshToken.Trim());

                if (storedToken == null || !storedToken.IsActive)
                    return Response<RefreshTokenResponseDto>.Failure(new RefreshTokenResponseDto(), "Invalid refresh token", 401);

                storedToken.RevokedOn = DateTime.UtcNow;
                var newAccessToken = await _jwtService.GenerateTokenAsync(user);
                var newRefreshToken = await _jwtService.GenerateRefreshToken();
                var refreshTokenDays = config.GetValue<int>("JWT:RefreshTokenDurationInDays");

                user.RefreshTokens.Add(new RefreshToken
                {
                    Token = newRefreshToken,
                    CreatedOn = DateTime.UtcNow,
                    ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenDays)
                });
                var updateResult = await userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return Response<RefreshTokenResponseDto>.Failure(
                        "Failed to update user refresh token",
                        500,
                        []
                    );
                }


                var data = new RefreshTokenResponseDto
                {
                    UserId = user.Id,
                    Name = user.UserName,
                    IsAuthenticated = true,
                    Token = newAccessToken.ToString(), //
                    RefreshToken = newRefreshToken,
                };

                return Response<RefreshTokenResponseDto>.Success(data, "Token refreshed successfully", 200);
            }
            catch (Exception)
            {
                return Response<RefreshTokenResponseDto>.Failure($"An unexpected error occurred.", 500, []);
            }
        }


        public async Task<Response<string>> LogoutAsync(string userId)
        {

            try
            {
                var user = await userManager.Users
                 .Include(u => u.RefreshTokens)
                 .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.IsActive)
                    return Response<string>.Failure("", "User not found", 404);

                foreach (var token in user.RefreshTokens)
                {
                    token.RevokedOn = DateTime.UtcNow;
                }

                await userManager.UpdateAsync(user);

                return Response<string>.Success("Done", "Logged out successfully", 200);
            }
            catch (Exception)
            {
                return Response<string>.Failure("", $"An unexpected error occurred.", 500, []);
            }

        }

        public async Task<Response<LogInResponseDTO>> LoginAsync(LoginRequestDto request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(), "Invalid credentials", 401);
            }

            var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);

            if (!passwordValid)
            {
                return Response<LogInResponseDTO>.Failure(new LogInResponseDTO(), "Invalid credentials", 401);
            }

            var newAccessToken = await _jwtService.GenerateTokenAsync(user);
            var newRefreshToken = await _jwtService.GenerateRefreshToken();
            var refreshTokenDays = config.GetValue<int>("JWT:RefreshTokenDurationInDays");

            user.RefreshTokens.Add(new RefreshToken
            {
                Token = newRefreshToken,
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(refreshTokenDays)
            });
            var updateResult = await userManager.UpdateAsync(user);



            return Response<LogInResponseDTO>
                .Success(
                    new LogInResponseDTO
                    {
                        UserId = user.Id,
                        Name = user.FullName,
                        Token = newAccessToken,
                        IsAuthenticated = true,
                        RefreshToken = newRefreshToken
                    },
                    "Login successful",
                    200);
        }





    }
}

