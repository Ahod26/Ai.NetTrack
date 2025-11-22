using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using backend.Models.Configuration;
using backend.Models.Domain;
using backend.Services.Interfaces.Auth;

namespace backend.Services.Classes.Auth;

public class TokenService(IOptions<JwtSettings> options) : ITokenService
{
  private readonly JwtSettings settings = options.Value;
  public string GenerateToken(ApiUser user, List<string> roles)
  {
    var claims = new List<Claim>
    {
      new Claim(ClaimTypes.NameIdentifier, user.Id),
      new Claim(ClaimTypes.Email, user.Email!),
      new Claim(ClaimTypes.Name, user.FullName!)
    };

    foreach (var role in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var expirationMinutes = settings.ExpirationInMinutes;
    var token = new JwtSecurityToken(
        settings.Issuer,
        settings.Audience,
        claims,
        expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
        signingCredentials: creds
    );
    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}