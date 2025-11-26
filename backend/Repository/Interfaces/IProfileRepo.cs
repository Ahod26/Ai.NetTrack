using backend.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace backend.Repository.Interfaces;

public interface IProfileRepo
{
  Task<IdentityResult> ChangeProfileEmailAsync(string userId, string newEmail);
  Task<IdentityResult> ChangeProfileFullNameAsync(string userId, string newName);
  Task<IdentityResult> ChangeProfilePasswordAsync(string userId, string newPassword, string currentPassword);
  Task<IdentityResult> DeleteProfileAsync(string userId);
  Task<ApiUser?> GetUserById(string userId);
}