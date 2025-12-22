using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services.Interfaces.Chat;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;
using backend.Extensions;


namespace backend.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("messages")]
[RequestTimeout(2000)]
public class MessagesController(IMessagesService messagesService)
: ControllerBase
{
  [HttpGet("starred")]
  public async Task<IActionResult> GetStarredMessages()
  {
      var userId = User.GetUserId();
      var messages = await messagesService.GetStarredMessagesAsync(userId);
      return Ok(messages);
  }

  [HttpPatch("{messageId:guid:required}/starred")]
  public async Task<IActionResult> ToggleStar(Guid messageId)
  {
      var userId = User.GetUserId();
      var result = await messagesService.ToggleStarAsync(messageId, userId);

      if (result.Message == null)
        return NotFound(new { message = "Message not found or does not belong to user" });

      return Ok(new { messageId, isStarred = result.IsStarred, message = "Star toggled successfully" });
  }

  [HttpPatch("{messageId:guid}/report")]
  public async Task<IActionResult> ReportMessage(Guid messageId, [FromBody] string reportReason)
  {
      var userId = User.GetUserId();
      var result = await messagesService.ReportMessageAsync(messageId, userId, reportReason);

      if (result)
        return Ok(new { message = "Message reported successfully" });

      return BadRequest();
  }
}

