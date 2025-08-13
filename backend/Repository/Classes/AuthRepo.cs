
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    Console.WriteLine("=== CreateAsync Debug Info ===");

    // Log input parameters
    Console.WriteLine($"User Email: {user?.Email ?? "NULL"}");
    Console.WriteLine($"User UserName: {user?.UserName ?? "NULL"}");
    Console.WriteLine($"User Id: {user?.Id ?? "NULL"}");
    Console.WriteLine($"Password provided: {!string.IsNullOrEmpty(password)}");
    Console.WriteLine($"Password length: {password?.Length ?? 0}");

    // Log user object state
    Console.WriteLine($"User EmailConfirmed: {user?.EmailConfirmed}");
    Console.WriteLine($"User PhoneNumber: {user?.PhoneNumber ?? "NULL"}");
    Console.WriteLine($"User LockoutEnabled: {user?.LockoutEnabled}");
    Console.WriteLine($"User TwoFactorEnabled: {user?.TwoFactorEnabled}");

    try
    {
      Console.WriteLine("Starting userManager.CreateAsync...");

      var result = await userManager.CreateAsync(user, password);

      Console.WriteLine($"CreateAsync completed. Success: {result.Succeeded}");

      if (!result.Succeeded)
      {
        Console.WriteLine("=== IDENTITY ERRORS ===");
        foreach (var error in result.Errors)
        {
          Console.WriteLine($"Error Code: {error.Code}");
          Console.WriteLine($"Error Description: {error.Description}");
          Console.WriteLine("---");
        }
      }
      else
      {
        Console.WriteLine("User created successfully!");
        Console.WriteLine($"Generated User ID: {user.Id}");
      }

      return result;
    }
    catch (Exception ex)
    {
      Console.WriteLine("=== EXCEPTION CAUGHT ===");
      Console.WriteLine($"Exception Type: {ex.GetType().Name}");
      Console.WriteLine($"Exception Message: {ex.Message}");

      if (ex.InnerException != null)
      {
        Console.WriteLine($"Inner Exception Type: {ex.InnerException.GetType().Name}");
        Console.WriteLine($"Inner Exception Message: {ex.InnerException.Message}");
      }

      Console.WriteLine($"Stack Trace: {ex.StackTrace}");
      Console.WriteLine("========================");

      throw;
    }
    finally
    {
      Console.WriteLine("=== CreateAsync Debug End ===");
    }
  }

  public async Task<IdentityResult> AddToRoleAsync(ApiUser user, string role)
  {
    return await userManager.AddToRoleAsync(user, role);
  }

  public async Task<ApiUser?> FindByUsernameAsync(string username)
  {
    return await userManager.FindByNameAsync(username);
  }
}