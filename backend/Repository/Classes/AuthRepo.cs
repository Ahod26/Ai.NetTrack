using Microsoft.AspNetCore.Identity;
using backend.Models.Domain;
using backend.Repository.Interfaces;

namespace backend.Repository.Classes;

public class AuthRepo(UserManager<ApiUser> userManager) : IAuthRepo
{
  public async Task<ApiUser?> FindByEmailAsync(string email)
  {
    return await userManager.FindByEmailAsync(email);
  }

  public async Task<bool> CheckPasswordAsync(ApiUser user, string password)
  {
    return await userManager.CheckPasswordAsync(user, password);
  }

  public async Task<List<string>> GetRolesAsync(ApiUser user)
  {
    var roles = await userManager.GetRolesAsync(user);
    return roles.ToList();
  }

  public async Task<IdentityResult> CreateAsync(ApiUser user, string password)
  {
    return await userManager.CreateAsync(user, password);
  }

  public async Task<IdentityResult> AddToRoleAsync(ApiUser user, string role)
  {
    return await userManager.AddToRoleAsync(user, role);
  }

}