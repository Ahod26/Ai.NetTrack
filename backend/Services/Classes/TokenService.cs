
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class TokenService(IConfiguration configuration) : ITokenService
{
  public string GenerateToken(ApiUser user, List<string> roles)
  {
    var claims = new List<Claim>
    {
      new Claim(ClaimTypes.NameIdentifier, user.Id),
      new Claim(ClaimTypes.Email, user.Email!)
    };

    foreach (var role in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var expirationMinutes = int.Parse(configuration["JwtSettings:ExpirationInMinutes"] ?? "1440");
    var token = new JwtSecurityToken(
        configuration["JwtSettings:Issuer"],
        configuration["JwtSettings:Audience"],
        claims,
        expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
        signingCredentials: creds
    );
    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}