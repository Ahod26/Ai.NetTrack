public interface ITokenService
{
  string GenerateToken(ApiUser user, List<string> roles);
}