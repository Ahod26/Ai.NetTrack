using backend.Models;
using backend.Models.Domain;
using backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace backend.E2ETests.Helpers;

/// <summary>
/// Helper for generating JWT tokens for E2E tests
/// </summary>
public static class TokenHelper
{
  public static async Task<string> GetTokenForUserAsync(
    E2EWebAppFactory factory,
    string email)
  {
    using var scope = factory.Services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();
    var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
      throw new InvalidOperationException($"User with email {email} not found");
    }

    var roles = await userManager.GetRolesAsync(user);
    var token = tokenService.GenerateToken(user, roles.ToList());

    return token;
  }
}
