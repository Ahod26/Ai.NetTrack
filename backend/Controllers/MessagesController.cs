using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Services.Interfaces.Chat;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;


namespace backend.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
[EnableRateLimiting("messages")]
[RequestTimeout(2000)]
public class MessagesController(IMessagesService messagesService, ILogger<MessagesController> logger)
: ControllerBase
{
  [HttpGet("starred")]
  public async Task<IActionResult> GetStarredMessages()
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var messages = await messagesService.GetStarredMessagesAsync(userId);
      return Ok(messages);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting all starred messages");
      return StatusCode(500, "Error getting starred messages");
    }
  }

  [HttpPatch("{messageId:guid:required}/starred")]
  public async Task<IActionResult> ToggleStar(Guid messageId)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var result = await messagesService.ToggleStarAsync(messageId, userId);
      return Ok(new { messageId, isStarred = result.IsStarred, message = "Star toggled successfully" });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error toggling star for {MessageId}", messageId);
      return StatusCode(500, "Error toggling star");
    }
  }

  [HttpPatch("{messageId:guid}/report")]
  public async Task<IActionResult> ReportMessage(Guid messageId, [FromBody] string reportReason)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var result = await messagesService.ReportMessageAsync(messageId, userId, reportReason);

      if (result)
        return Ok(new { message = "Message reported successfully" });

      return BadRequest();
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error setting reported message for {messageId}", messageId);
      return StatusCode(500, "Error setting reported message");
    }
  }
}

