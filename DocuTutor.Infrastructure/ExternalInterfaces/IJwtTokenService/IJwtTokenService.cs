
using DocuTutor.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DocuTutor.Infrastructure.ExternalInterfaces.IJwtTokenService
{
    public interface IJwtTokenService
    {
      Task<string> GenerateTokenAsync(ApplicationUser user);
      public Task<string> GenerateRefreshToken();
      public Task<ClaimsPrincipal?> GetPrincipalFromExpiredToken(string token);


        //Task<JwtSecurityToken> GenerateJwtToken(ApplicationUser User);
        RefreshToken CreateRefreshToken();


    }
}
