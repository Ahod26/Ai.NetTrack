using backend.Models.Domain;

namespace backend.Services.Interfaces.Auth;

public interface ITokenService
{
  string GenerateToken(ApiUser user, List<string> roles);
}