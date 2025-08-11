using System.Security.Claims;

public interface IAuthService
{
  Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO);
  Task<RegisterResponseDTO> RegisterAsync(RegisterDTO registerDTO);
  UserInfoDTO GetCurrentUserFromClaims(ClaimsPrincipal user);
}