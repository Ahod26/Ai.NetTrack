using backend.Models.Domain;

namespace backend.Models.Dtos;

public class CachedChatData
{
  public ChatMetaDataDto? Metadata { get; set; }
  public List<ChatMessage>? Messages { get; set; }
}