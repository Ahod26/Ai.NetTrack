using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ChatController(IChatService chatService, ILogger<ChatController> logger) : ControllerBase
{
  [HttpPost]
  [TypeFilter(typeof(MaxChatsAttribute))]
  public async Task<IActionResult> CreateChat([FromBody] CreateChatDTO createChatDTO, [FromQuery] int? timezoneOffset = null)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chat = await chatService.CreateChatAsync(userId, createChatDTO.FirstMessage, timezoneOffset);

      return Ok(chat);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error creating chat");
      return StatusCode(500, "Error creating chat");
    }
  }

  [HttpGet]
  public async Task<IActionResult> GetUserChatsMetaData([FromQuery] int? timezoneOffset = null)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chats = await chatService.GetUserChatsAsync(userId, timezoneOffset);

      return Ok(chats);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting user chats");
      return StatusCode(500, "Error retrieving chats");
    }
  }

  [HttpGet("{chatId:guid:required}")]
  public async Task<IActionResult> GetChat(Guid chatId, [FromQuery] int? timezoneOffset = null)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chat = await chatService.GetUserChatAsync(chatId, userId, timezoneOffset);

      if (chat == null)
      {
        return NotFound("Chat not found");
      }

      return Ok(chat);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting chat");
      return StatusCode(500, "Error retrieving chat");
    }
  }

  [HttpDelete("{chatId:guid:required}")]
  public async Task<IActionResult> DeleteChat(Guid chatId)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chat = await chatService.GetUserChatAsync(chatId, userId);
      if (chat == null)
      {
        return NotFound("Chat not found");
      }

      await chatService.DeleteChatByIdAsync(chatId);

      return Ok(new { message = "Chat deleted successfully" });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error deleting chat {ChatId}", chatId);
      return StatusCode(500, "Error deleting chat");
    }
  }

  [HttpPatch("{chatId:guid:required}/title")]
  public async Task<IActionResult> ChangeChatTitle(Guid chatId,
  [FromBody]
  [Required(ErrorMessage = "Title is required")]
  [StringLength(20, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 20 characters")]
  string title)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chat = await chatService.GetUserChatAsync(chatId, userId);
      if (chat == null)
      {
        return NotFound("Chat not found");
      }

      await chatService.ChangeChatTitle(chatId, title);

      return Ok(new { message = "Title changed successfully" });
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error changing title for chat {ChatId}", chatId);
      return StatusCode(500, "Error changing title");
    }
  }
}