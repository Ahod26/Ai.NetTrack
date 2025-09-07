using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using backend.Repository.Interfaces;
using backend.Models.Dtos;
using backend.Models.Domain;

using backend.Services.Interfaces.Auth;

namespace backend.Services.Classes.Auth;

public class AuthService(ICookieService cookieService, ITokenService tokenService,
  IAuthRepo authRepo) : IAuthService
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
          {
            Success = true,
            Message = "Login successful",
            User = new UserInfoDTO
            {
              UserName = user.UserName!,
              Email = user.Email!,
              Roles = roles.ToList()
            }
          };
        }
      }
    }

    return new LoginResponseDTO
    {
      Success = false,
      Message = "Invalid email or password"
    };
  }

  public async Task<RegisterResponseDTO> RegisterAsync(RegisterDTO registerDTO)
  {
    // Check for existing email first
    var existingUserByEmail = await authRepo.FindByEmailAsync(registerDTO.Email);
    if (existingUserByEmail != null)
    {
      return new RegisterResponseDTO
      {
        Success = false,
        Message = "Registration failed",
        Errors = new List<string> { "Email address is already registered" }
      };
    }

    // Check for existing username
    var existingUserByUsername = await authRepo.FindByUsernameAsync(registerDTO.UserName);
    if (existingUserByUsername != null)
    {
      return new RegisterResponseDTO
      {
        Success = false,
        Message = "Registration failed",
        Errors = new List<string> { "Username is already taken" }
      };
    }

    var applicationUser = new ApiUser
    {
      UserName = registerDTO.UserName,
      Email = registerDTO.Email
    };

    var identityResult = await authRepo.CreateAsync(applicationUser, registerDTO.Password);

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
      {
        Success = false,
        Message = "Registration failed",
        Errors = errors
      };
    }

    // Try to add role
    var roleResult = await authRepo.AddToRoleAsync(applicationUser, "premium");
    if (!roleResult.Succeeded)
    {
      return new RegisterResponseDTO
      {
        Success = false,
        Message = "User created but role assignment failed",
        Errors = roleResult.Errors.Select(e => e.Description).ToList()
      };
    }

    return new RegisterResponseDTO
    {
      Success = true,
      Message = "Registration successful",
      Errors = new List<string>()
    };
  }

  public UserInfoDTO GetCurrentUserFromClaims(ClaimsPrincipal user)
  {
    return new UserInfoDTO
    {
      UserName = user.FindFirst(ClaimTypes.Name)?.Value ?? "",
      Email = user.FindFirst(ClaimTypes.Email)?.Value ?? "",
      Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
    };
  }
}