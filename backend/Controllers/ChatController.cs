using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ChatController(IChatService chatService, ILogger<ChatController> logger) : ControllerBase
{
  [HttpPost]
  public async Task<IActionResult> CreateChat([FromBody] CreateChatDTO createChatDTO)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chat = await chatService.CreateChatAsync(userId, createChatDTO.Title);

      return Ok(chat);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error creating chat");
      return StatusCode(500, "Error creating chat");
    }
  }

  [HttpGet]
  public async Task<IActionResult> GetUserChats()
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chats = await chatService.GetUserChatsAsync(userId);

      return Ok(chats);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting user chats");
      return StatusCode(500, "Error retrieving chats");
    }
  }

  [HttpGet("{chatId}")]
  public async Task<IActionResult> GetChat(Guid chatId)
  {
    try
    {
      var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;
      var chat = await chatService.GetUserChatAsync(chatId, userId);

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
}