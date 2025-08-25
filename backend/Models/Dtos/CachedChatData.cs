public class CachedChatData
{
  public required ChatMetaDataDto Metadata { get; set; }
  public required List<ChatMessage> messages { get; set; }
}