using Microsoft.AspNetCore.Identity;

public class AuthService(ICookieService cookieService, ITokenService tokenService,
  IAuthRepo authRepo) : IAuthService
{
  public async Task<string> LoginAsync(LoginDTO loginDTO)
  {
    var user = await authRepo.FindByEmailAsync(loginDTO.Email);
    if (user != null)
    {
      var checkPasswordResult = await authRepo.CheckPasswordAsync(user, loginDTO.Password);
      if (checkPasswordResult)
      {
        var roles = await authRepo.GetRolesAsync(user);
        if (roles.Any())
        {
          var jwtToken = tokenService.GenerateToken(user, roles.ToList());
          cookieService.SetAuthCookie(jwtToken);

          return jwtToken;
        }
      }
    }
    return "";
  }

  public async Task<bool> RegisterAsync(RegisterDTO registerDTO)
  {
    var applicationUser = new ApiUser
    {
      UserName = registerDTO.UserName,
      Email = registerDTO.Email
    };
    var identityResult = await authRepo.CreateAsync(applicationUser, registerDTO.Password);

    if (identityResult.Succeeded)
    {
      identityResult = await authRepo.AddToRoleAsync(applicationUser, "premium");
      if (identityResult.Succeeded)
      {
        return true;
      }
    }
    return false;
  }
}