using backend.Models.Domain;
using Microsoft.AspNetCore.Identity;

namespace backend.Repository.Interfaces;

public interface IProfileRepo
{
  Task<(string userFullName, IdentityResult identityResult)> ChangeProfileEmailAsync(string userId, string newEmail);
  Task<(string userEmail, IdentityResult identityResult)> ChangeProfileFullNameAsync(string userId, string newName);
  Task<IdentityResult> ChangeProfilePasswordAsync(string userId, string newPassword, string currentPassword);
  Task<(string userEmail, IdentityResult identityResult)> DeleteProfileAsync(string userId);
  Task<ApiUser?> GetUserById(string userId);
  Task<(string userEmail, string userFullName, IdentityResult identityResult)> UpdateUserNewsletterPreferenceAsync(string userId);
}