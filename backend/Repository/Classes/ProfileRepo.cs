using backend.Models.Domain;
using backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace backend.Repository.Classes;

public class ProfileRepo(UserManager<ApiUser> userManager) : IProfileRepo
{
  public async Task<(string userFullName, IdentityResult identityResult)> ChangeProfileEmailAsync(string userId, string newEmail)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return  ("", IdentityResult.Failed(new IdentityError { Description = "User not found" }));

    user.Email = newEmail;
    user.UserName = newEmail;
    user.EmailConfirmed = false;

    var identityRes = await userManager.UpdateAsync(user);
    return (user.FullName, identityRes);
  }

  public async Task<(string userEmail, IdentityResult identityResult)> ChangeProfileFullNameAsync(string userId, string newName)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return ("", IdentityResult.Failed(new IdentityError { Description = "User not found" }));
    user.FullName = newName;
    var identityRes = await userManager.UpdateAsync(user);
    return (user.Email!, identityRes);
  }

  public async Task<IdentityResult> ChangeProfilePasswordAsync(string userId, string newPassword, string currentPassword)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return IdentityResult.Failed(new IdentityError { Description = "User not found" });

    return await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
  }

  public async Task<(string userEmail, IdentityResult identityResult)> DeleteProfileAsync(string userId)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return ("", IdentityResult.Failed(new IdentityError { Description = "User not fount" }));

    var identityRes = await userManager.DeleteAsync(user);
    return (user.Email!, identityRes);
  }

  public async Task<ApiUser?> GetUserById(string userId)
  {
    return await userManager.FindByIdAsync(userId);
  }

  public async Task<(string userEmail, string userFullName, IdentityResult identityResult)> UpdateUserNewsletterPreferenceAsync(string userId)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return ("", "", IdentityResult.Failed(new IdentityError { Description = "User not found" }));

    user.IsSubscribedToNewsletter = !user.IsSubscribedToNewsletter;
    return (user.Email!, user.FullName, await userManager.UpdateAsync(user));
  }
}