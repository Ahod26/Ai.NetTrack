using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Models.Dtos;
using backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using backend.Extensions;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[EnableRateLimiting("auth")]
public class AuthController
(ILogger<AuthController> logger, IAuthService authService) : ControllerBase
{
  [HttpPost("")]
  public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
  {
    logger.LogInformation("Register attempt for email: {Email}", registerDTO.Email);

      var result = await authService.RegisterAsync(registerDTO);

      if (result.Success)
      {
        logger.LogInformation("Registration successful for email: {Email}", registerDTO.Email);
        return Ok(result);
      }

      logger.LogWarning("Registration failed for email: {Email}. Errors: {Errors}",
          registerDTO.Email, string.Join(", ", result.Errors));

      return BadRequest(result);
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
  {
    logger.LogInformation("Login attempt for email: {Email}", loginDTO.Email);

      var result = await authService.LoginAsync(loginDTO);
      if (result.Success)
      {
        logger.LogInformation("Login successful for email: {Email}", loginDTO.Email);
        return Ok(result);
      }

      logger.LogWarning("Login failed for email: {Email}", loginDTO.Email);
      return Unauthorized(new { message = result.Message });
  }

  [HttpGet("status")]
  [Authorize]
  public IActionResult UserStatusCheck()
  {
      var userInfo = authService.GetCurrentUserFromClaims(User);

      return Ok(new
      {
        isAuthenticated = true,
        user = userInfo
      });
  }

  [HttpPost("logout")]
  [Authorize]
  public IActionResult Logout()
  {
    Response.Cookies.Delete("authToken", new CookieOptions
    {
      HttpOnly = true,
      Secure = true,
      SameSite = SameSiteMode.None,
      Path = "/"
    });
    return Ok(new { message = "Logout successful" });
  }

  [HttpGet("google-login")]
  public IActionResult GoogleLogin()
  {
    var redirectUrl = Url.Action("GoogleResponse", "Auth");
    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
    return Challenge(properties, GoogleDefaults.AuthenticationScheme);
  }

  [HttpGet("google-response")]
  public async Task<IActionResult> GoogleResponse()
  {
    var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);

    var loginResult = await authService.GoogleLoginAsync(result);

    if (!loginResult.Success)
      return Redirect($"http://localhost:5173/auth/callback?success=false&error={Uri.EscapeDataString(loginResult.Message)}");

    return Redirect($"http://localhost:5173/auth/callback?success=true");
  }
}