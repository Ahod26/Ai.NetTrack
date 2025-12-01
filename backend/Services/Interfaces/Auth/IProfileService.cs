using backend.Models.Dtos;
using Microsoft.AspNetCore.Identity;

namespace backend.Services.Interfaces;

public interface IProfileService
{
  Task<IdentityResult> ChangeEmailAsync(string newEmail, string userId);
  Task<IdentityResult> ChangeFullNameAsync(string newName, string userId);
  Task<IdentityResult> ChangePasswordAsync(string newPassword, string currentPassword, string userId);
  Task<IdentityResult> DeleteUserAsync(string userId);
  Task<UserInfoDTO?> UpdateJWT(string userId);
  Task<IdentityResult> UpdateUserNewsletterPreferenceAsync(string userId);
}