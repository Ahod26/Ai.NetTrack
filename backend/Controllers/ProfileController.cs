using System.Security.Claims;
using backend.Models.Dtos;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Build.Framework;
using ModelContextProtocol.Protocol;
using backend.Extensions;

namespace backend.Controllers;

[Route("[controller]")]
[Authorize]
[ApiController]
[EnableRateLimiting("profile")]
public class ProfileController(
  IProfileService profileService) : ControllerBase
{
  [HttpPut("email")]
  public async Task<IActionResult> UpdateProfileEmail(UpdateProfileEmailDTO newEmail)
  {
      string userId = User.GetUserId();
      var res = await profileService.ChangeEmailAsync(newEmail.Email, userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      var userInfo = await profileService.UpdateJWT(userId);
      
      return Ok(userInfo);
  }

  [HttpPut("username")]
  public async Task<IActionResult> UpdateProfileFullName(UpdateProfileFullNameDTO newName)
  {
      string userId = User.GetUserId();
      var res = await profileService.ChangeFullNameAsync(newName.FullName, userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      var userInfo = await profileService.UpdateJWT(userId);
      
      return Ok(userInfo);
  }

  [HttpPut("password")]
  public async Task<IActionResult> UpdateProfilePassword(UpdateProfilePasswordDTO newPassword)
  {
      string userId = User.GetUserId();
      var res = await profileService.ChangePasswordAsync(newPassword.NewPassword, newPassword.CurrentPassword, userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      return Ok();
  }

  [HttpPut("newsletter")]
  public async Task<IActionResult> UpdateNewsletterPreference()
  {
      string userId = User.GetUserId();
      var res = await profileService.UpdateUserNewsletterPreferenceAsync(userId);

      if (!res.Succeeded)
        return BadRequest(res.Errors);

      var userInfo = await profileService.UpdateJWT(userId);
      
      return Ok(userInfo);
  }

  [HttpDelete("")]
  public async Task<IActionResult> DeleteProfile()
  {
    string userId = User.GetUserId();
    var res = await profileService.DeleteUserAsync(userId);

    if (!res.Succeeded)
      return BadRequest(res.Errors);

    return Ok();
  }
}

