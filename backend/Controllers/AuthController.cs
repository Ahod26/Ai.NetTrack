
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class AuthController(ILogger<AuthController> logger, IAuthService authService) : ControllerBase
{
  [HttpPost("")]
  public async Task<IActionResult> Register([FromBody] RegisterDTO registerDTO)
  {
    logger.LogInformation("Register attempt for email: {Email}", registerDTO.Email);

    try
    {
      var result = await authService.RegisterAsync(registerDTO);
      if (result)
      {
        logger.LogInformation("Registration successful for email: {Email}", registerDTO.Email);
        return Ok("user created");
      }

      logger.LogWarning("Registration failed for email: {Email}", registerDTO.Email);
      return BadRequest();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Registration error for email: {Email}", registerDTO.Email);
      return BadRequest();
    }
  }

  [HttpPost("login")]
  public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
  {
    logger.LogInformation("Login attempt for email: {Email}", loginDTO.Email);

    try
    {
      var result = await authService.LoginAsync(loginDTO);
      if (result != "")
      {
        logger.LogInformation("Login successful for email: {Email}", loginDTO.Email);
        return Ok("logged in");
      }

      logger.LogWarning("Login failed for email: {Email}", loginDTO.Email);
      return BadRequest();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Login error for email: {Email}", loginDTO.Email);
      return BadRequest();
    }
  }

  [HttpGet("status")]
  [Authorize]
  public IActionResult UserStatusCheck()
  {
    return Ok();
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