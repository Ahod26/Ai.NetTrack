using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class ChatHub(IChatService chatService) : Hub<IChatClient>
{
  //reconnecting for user that created chats/groups already 
  public async Task JoinChat(string chatId)
  {
    var userId = Context.UserIdentifier!;

    var chat = await chatService.GetUserChatAsync(Guid.Parse(chatId), userId);
    if (chat == null)
    {
      await Clients.Caller.Error("Chat not found");
      return;
    }

    await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");

    var messages = await chatService.GetChatMessagesAsync(Guid.Parse(chatId));
    await Clients.Caller.ChatJoined(chatId, chat.Title, messages);
  }

  public async Task SendMessage(string chatId, string content)
  {
    var userId = Context.UserIdentifier!;

    // Security check
    var chat = await chatService.GetUserChatAsync(Guid.Parse(chatId), userId);
    if (chat == null)
    {
      await Clients.Caller.Error("Access denied");
      return;
    }

    // Send user message to group immediately
    await Clients.Group($"Chat_{chatId}").ReceiveMessage(new
    {
      Type = "User",
      Content = content,
      CreatedAt = DateTime.UtcNow
    });

    // Process user message and get AI response
    var aiMessage = await chatService.ProcessUserMessageAsync(Guid.Parse(chatId), content);

    // Send AI response to group
    await Clients.Group($"Chat_{chatId}").ReceiveMessage(new
    {
      Id = aiMessage.Id,
      Type = "Assistant",
      Content = aiMessage.Content,
      CreatedAt = aiMessage.CreatedAt
    });
  }
}