using System.Security.Claims;
using backend.Models.Dtos;
using Microsoft.AspNetCore.Authentication;

namespace backend.Services.Interfaces.Auth;

public interface IAuthService
{
  Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO);
  Task<RegisterResponseDTO> RegisterAsync(RegisterDTO registerDTO);
  UserInfoDTO GetCurrentUserFromClaims(ClaimsPrincipal user);
  Task<LoginResponseDTO> GoogleLoginAsync(AuthenticateResult googleAuthResult);
}