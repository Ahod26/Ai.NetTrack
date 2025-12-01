using System.Security.Claims;
using backend.Models.Dtos;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Build.Framework;
using ModelContextProtocol.Protocol;

namespace backend.Controllers;

[Route("[controller]")]
[Authorize]
[ApiController]
[EnableRateLimiting("profile")]
public class ProfileController(
  IProfileService profileService,
  ILogger<ProfileController> logger) : ControllerBase
{
  [HttpPut("email")]
  public async Task<IActionResult> UpdateProfileEmail(UpdateProfileEmailDTO newEmail)
  {
    try
    {
      string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var res = await profileService.ChangeEmailAsync(newEmail.Email, userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      var userInfo = await profileService.UpdateJWT(userId);
      
      return Ok(userInfo);
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Error Update email: {Email}", newEmail.Email);
      return StatusCode(500, new { message = "An error occurred changing email address" });
    }
  }

  [HttpPut("username")]
  public async Task<IActionResult> UpdateProfileFullName(UpdateProfileFullNameDTO newName)
  {
    try
    {
      string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var res = await profileService.ChangeFullNameAsync(newName.FullName, userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      var userInfo = await profileService.UpdateJWT(userId);
      
      return Ok(userInfo);
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Error Update full name: {FullName}", newName.FullName);
      return StatusCode(500, new { message = "An error occurred changing email address" });
    }
  }

  [HttpPut("password")]
  public async Task<IActionResult> UpdateProfilePassword(UpdateProfilePasswordDTO newPassword)
  {
    try
    {
      string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var res = await profileService.ChangePasswordAsync(newPassword.NewPassword, newPassword.CurrentPassword, userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      return Ok();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error Update password");
      return StatusCode(500, new { message = "An error occurred changing password" });
    }
  }

  [HttpPut("newsletter")]
  public async Task<IActionResult> UpdateNewsletterPreference()
  {
    try
    {
      string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var res = await profileService.UpdateUserNewsletterPreferenceAsync(userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      var userInfo = await profileService.UpdateJWT(userId);
      
      return Ok(userInfo);
    }
    catch(Exception ex)
    {
      logger.LogError(ex, "Error updating newsletter preference");
      return StatusCode(500, new { message = "An error occurred" });
    }
  }

  [HttpDelete("")]
  public async Task<IActionResult> DeleteProfile()
  {
    string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
    var res = await profileService.DeleteUserAsync(userId);

    if (!res.Succeeded)
      return BadRequest(res.Errors);

    return Ok();
  }
}

