using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using backend.Repository.Interfaces;
using backend.Models.Dtos;
using backend.Models.Domain;

using backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Authentication;
using AutoMapper;
using backend.Services.Interfaces;
using Org.BouncyCastle.Crypto.Engines;

namespace backend.Services.Classes.Auth;

public class AuthService(
  ICookieService cookieService,
  ITokenService tokenService,
  IAuthRepo authRepo,
  IMapper mapper,
  IEmailListCacheService emailListCacheService,
  ILogger<AuthService> logger) : IAuthService
{
  public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO)
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

          return new LoginResponseDTO
          (
            Success: true,
            Message: "Login successful",
            UserInfo: new UserInfoDTO
            (
              ApiUserDto: mapper.Map<ApiUserDto>(user),
              Roles: roles.ToList()
            )
          );
        }
      }
    }

    return new LoginResponseDTO
    (
      Success: false,
      UserInfo: null,
      Message: "Invalid email or password"
    );
  }

  public async Task<RegisterResponseDTO> RegisterAsync(RegisterDTO registerDTO)
  {
    // Check for existing email 
    var existingUserByEmail = await authRepo.FindByEmailAsync(registerDTO.Email);
    if (existingUserByEmail != null)
    {
      return new RegisterResponseDTO
      (
        Success: false,
        Message: "Registration failed",
        Errors: new List<string> { "Email address is already registered" }
      );
    }

    var applicationUser = new ApiUser
    {
      UserName = registerDTO.Email,
      Email = registerDTO.Email,
      FullName = registerDTO.FullName,
      IsSubscribedToNewsletter = registerDTO.IsSubscribedToNewsletter
    };

    var identityResult = await authRepo.CreateAsync(applicationUser, registerDTO.Password);

    if (registerDTO.IsSubscribedToNewsletter)
      await emailListCacheService.ToggleUserFromNewsletterAsync(new EmailNewsletterDTO
      (
        Email: registerDTO.Email,
        FullName: registerDTO.FullName
      ));

    if (!identityResult.Succeeded)
    {
      // Handle password and other validation errors
      var errors = new List<string>();

      foreach (var error in identityResult.Errors)
      {
        switch (error.Code)
        {
          case "PasswordTooShort":
            errors.Add("Password must be at least 6 characters long");
            break;
          case "PasswordRequiresDigit":
            errors.Add("Password must contain at least one number");
            break;
          case "PasswordRequiresLower":
            errors.Add("Password must contain at least one lowercase letter");
            break;
          case "PasswordRequiresUpper":
            errors.Add("Password must contain at least one uppercase letter");
            break;
          case "PasswordRequiresNonAlphanumeric":
            errors.Add("Password must contain at least one special character");
            break;
          default:
            errors.Add(error.Description);
            break;
        }
      }

      return new RegisterResponseDTO
      (
        Success: false,
        Message: "Registration failed",
        Errors: errors
      );
    }

    // Try to add role
    var roleResult = await authRepo.AddToRoleAsync(applicationUser, "premium");
    if (!roleResult.Succeeded)
    {
      return new RegisterResponseDTO
      (
        Success: false,
        Message: "User created but role assignment failed",
        Errors: roleResult.Errors.Select(e => e.Description).ToList()
      );
    }

    return new RegisterResponseDTO
    (
      Success: true,
      Message: "Registration successful",
      Errors: new List<string>()
    );
  }

  public UserInfoDTO GetCurrentUserFromClaims(ClaimsPrincipal user)
  {
    logger.LogError(user.FindFirst("IsNewsletterSubscribed")?.Value);
    return new UserInfoDTO
    (
      Roles: user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
      ApiUserDto: new ApiUserDto
      (
        FullName: user.FindFirst(ClaimTypes.Name)?.Value ?? "",
        Email: user.FindFirst(ClaimTypes.Email)?.Value ?? "",
        IsSubscribedToNewsletter: user.FindFirst("IsNewsletterSubscribed")?.Value == "True"
      )
    );
  }

  public async Task<LoginResponseDTO> GoogleLoginAsync(AuthenticateResult googleAuthResult)
  {
    if (!googleAuthResult.Succeeded)
    {
      return new LoginResponseDTO
      (
        Success: false,
        UserInfo: null,
        Message: "Google authentication failed"
      );
    }

    var claims = googleAuthResult.Principal.Claims;
    var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
    var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
    var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

    logger.LogInformation("Looking up user by email: {Email}", email);
    var user = await authRepo.FindByEmailAsync(email ?? "");

    logger.LogInformation("FindByEmailAsync result: {Result}, Email searched: '{Email}'",
    user == null ? "NULL" : $"Found user ID: {user.Id}", email);

    if (user == null)
    {
      user = new ApiUser
      {
        UserName = email,  // Use email as username for uniqueness
        Email = email,
        FullName = name ?? "",  // Store display name in FullName
        EmailConfirmed = true, // Google verified it
        IsSubscribedToNewsletter = true
      };

      var createResult = await authRepo.CreateAsync(user, Guid.NewGuid().ToString()); // Random password since they use Google

      if (!createResult.Succeeded)
      {
        return new LoginResponseDTO
        (
          Success: false,
          UserInfo: null,
          Message: "Failed to create user account"
        );
      }

      await authRepo.AddToRoleAsync(user, "premium");
    }

    var roles = await authRepo.GetRolesAsync(user);
    var jwtToken = tokenService.GenerateToken(user, roles.ToList());
    cookieService.SetAuthCookie(jwtToken);

    await emailListCacheService.ToggleUserFromNewsletterAsync(new EmailNewsletterDTO
    (
      Email: user.Email!,
      FullName: user.FullName
    ));

    return new LoginResponseDTO
    (
      Success: true,
      Message: "Login successful",
      UserInfo: new UserInfoDTO
      (
        Roles: roles.ToList(),
        ApiUserDto: mapper.Map<ApiUserDto>(user)
      )
    );
  }

}