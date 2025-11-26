using System.Net.WebSockets;
using backend.Models.Dtos;
using backend.Repository.Interfaces;
using backend.Services.Interfaces;
using backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Identity;
using OpenAI.Audio;

namespace backend.Services.Classes;

public class ProfileService(
  IProfileRepo profileRepo,
  ITokenService tokenService,
  IAuthRepo authRepo,
  ICookieService cookieService) : IProfileService
{
  public async Task<IdentityResult> ChangeEmailAsync(string newEmail, string userId)
  {
    return await profileRepo.ChangeProfileEmailAsync(userId, newEmail);
  }
  public async Task<IdentityResult> ChangeFullNameAsync(string newName, string userId)
  {
    return await profileRepo.ChangeProfileFullNameAsync(userId, newName);
  }
  public async Task<IdentityResult> ChangePasswordAsync(string newPassword, string currentPassword, string userId)
  {
    return await profileRepo.ChangeProfilePasswordAsync(userId, newPassword, currentPassword);
  }
  public async Task<IdentityResult> DeleteUserAsync(string userId)
  {
    return await profileRepo.DeleteProfileAsync(userId);
  }

  public async Task<UserInfoDTO?> UpdateJWT(string userId)
  {
    var user = await profileRepo.GetUserById(userId);
    
    if (user == null)
      return null;

    var roles = await authRepo.GetRolesAsync(user);
    if (roles.Any())
    {
      var jwtToken = tokenService.GenerateToken(user, roles.ToList());
      cookieService.SetAuthCookie(jwtToken);
      return new UserInfoDTO
      {
        FullName = user.FullName,
        Email = user.Email ?? "",
        Roles = roles.ToList()
      };
    }
    return null;
  }
}