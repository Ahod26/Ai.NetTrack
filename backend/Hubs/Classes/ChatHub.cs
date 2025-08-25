using System.Text;
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

    var messages = await chatService.GetAllChatMessagesAsync(Guid.Parse(chatId), userId);
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

    // Stream AI response chunks to client
    var aiMessage = await chatService.ProcessUserMessageAsync(
        Guid.Parse(chatId),
        content,
        userId,
        async (chunk) =>
        {
          await Clients.Group($"Chat_{chatId}").ReceiveMessage(new ChunkMessageDto { Content = chunk });
        }
    );

    await Clients.Group($"Chat_{chatId}").ReceiveMessage(aiMessage);
  }
}