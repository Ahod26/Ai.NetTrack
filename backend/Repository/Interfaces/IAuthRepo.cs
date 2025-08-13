using Microsoft.AspNetCore.Identity;

public interface IAuthRepo
{
  Task<ApiUser?> FindByEmailAsync(string email);
  Task<bool> CheckPasswordAsync(ApiUser user, string password);
  Task<List<string>> GetRolesAsync(ApiUser user);
  Task<IdentityResult> CreateAsync(ApiUser user, string password);
  Task<IdentityResult> AddToRoleAsync(ApiUser user, string role);
  Task<ApiUser?> FindByUsernameAsync(string username);
  
}