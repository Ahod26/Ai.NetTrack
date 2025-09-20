using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Models.Dtos;
using backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.RateLimiting;

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

    try
    {
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
    catch (Exception ex)
    {
      logger.LogError(ex, "Registration error for email: {Email}", registerDTO.Email);
      return StatusCode(500, new { message = "An error occurred during registration" });
    }
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
  {
    logger.LogInformation("Login attempt for email: {Email}", loginDTO.Email);

    try
    {
      var result = await authService.LoginAsync(loginDTO);
      if (result.Success)
      {
        logger.LogInformation("Login successful for email: {Email}", loginDTO.Email);
        return Ok(result);
      }

      logger.LogWarning("Login failed for email: {Email}", loginDTO.Email);
      return Unauthorized(new { message = result.Message });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Login error for email: {Email}", loginDTO.Email);
      return StatusCode(500, new { message = "An error occurred during login" });
    }
  }

  [HttpGet("status")]
  [Authorize]
  public IActionResult UserStatusCheck()
  {
    try
    {
      var userInfo = authService.GetCurrentUserFromClaims(User);

      return Ok(new
      {
        isAuthenticated = true,
        user = userInfo
      });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting user status");
      return BadRequest(new { message = "Error retrieving user information" });
    }
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

}