using System.Formats.Asn1;
using System.Reflection.Metadata.Ecma335;
using backend.Data;
using backend.Models.Domain;
using backend.Models.Dtos;
using backend.Repository.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace backend.Repository.Classes;

public class ProfileRepo (UserManager<ApiUser> userManager) : IProfileRepo
{
  public async Task<IdentityResult> ChangeProfileEmailAsync(string userId, string newEmail)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return IdentityResult.Failed(new IdentityError { Description = "User not found" }); ;

    user.Email = newEmail;
    return await userManager.UpdateAsync(user);
  }

  public async Task<IdentityResult> ChangeProfileFullNameAsync(string userId, string newName)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return IdentityResult.Failed(new IdentityError { Description = "User not found" });
    user.FullName = newName;
    return await userManager.UpdateAsync(user);
  }

  public async Task<IdentityResult> ChangeProfilePasswordAsync(string userId, string newPassword, string currentPassword)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return IdentityResult.Failed(new IdentityError { Description = "User not found" });

    return await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
  }

  public async Task<IdentityResult> DeleteProfileAsync(string userId)
  {
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
      return IdentityResult.Failed(new IdentityError { Description = "User not fount" });

    return await userManager.DeleteAsync(user);
  }
  
  public async Task<ApiUser?> GetUserById(string userId)
  {
    return await userManager.FindByIdAsync(userId);
  }
}