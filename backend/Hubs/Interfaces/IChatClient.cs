public interface IChatClient
{
  Task ReceiveMessage(object message);
  Task ChatJoined(string chatId, string title, List<ChatMessage> messages);
  Task ChatLeft(string chatId);
  Task Error(string error);
}