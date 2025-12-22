using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Models.Dtos;
using backend.Filters;
using backend.Services.Interfaces.Chat;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Timeouts;
using backend.Extensions;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
[EnableRateLimiting("chat")]
[RequestTimeout(5000)] 
public class ChatController
(IChatService chatService) : ControllerBase
{
  [HttpPost]
  [TypeFilter(typeof(MaxChatsAttribute))]
  public async Task<IActionResult> CreateChat([FromBody] CreateChatDTO createChatDTO, [FromQuery] int? timezoneOffset = null, [FromQuery] string? relatedNewsSource = null)
  {
      var userId = User.GetUserId();
      var chat = await chatService.CreateChatAsync(userId, createChatDTO.FirstMessage, timezoneOffset, relatedNewsSource);

      return Ok(chat);
  }

  [HttpGet]
  public async Task<IActionResult> GetUserChatsMetaData([FromQuery] int? timezoneOffset = null)
  {
      var userId = User.GetUserId();
      var chats = await chatService.GetUserChatsMetadataAsync(userId, timezoneOffset);

      return Ok(chats);
  }

  [HttpDelete("{chatId:guid:required}")]
  public async Task<IActionResult> DeleteChat(Guid chatId)
  {
      var userId = User.GetUserId();
      var chat = await chatService.GetUserChatAsync(chatId, userId);
      if (chat == null)
      {
        return NotFound("Chat not found");
      }

      await chatService.DeleteChatByIdAsync(chatId, userId);

      return Ok(new { message = "Chat deleted successfully" });
  }

  [HttpPatch("{chatId:guid:required}/title")]
  public async Task<IActionResult> ChangeChatTitle(Guid chatId,
  [FromBody]
  [Required(ErrorMessage = "Title is required")]
  [StringLength(20, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 20 characters")]
  string title)
  {
      var userId = User.GetUserId();
      var chat = await chatService.GetUserChatAsync(chatId, userId);
      if (chat == null)
      {
        return NotFound("Chat not found");
      }

      await chatService.ChangeChatTitle(chatId, title, userId);

      return Ok(new { message = "Title changed successfully" });
  }

}