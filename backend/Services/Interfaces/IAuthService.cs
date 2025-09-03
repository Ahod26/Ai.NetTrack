using System.Security.Claims;
using backend.Models.Dtos;

namespace backend.Services.Interfaces;

public interface IAuthService
{
  Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO);
  Task<RegisterResponseDTO> RegisterAsync(RegisterDTO registerDTO);
  UserInfoDTO GetCurrentUserFromClaims(ClaimsPrincipal user);
}