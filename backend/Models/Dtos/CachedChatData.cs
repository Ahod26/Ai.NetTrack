using backend.Models.Domain;

namespace backend.Models.Dtos;

public sealed record CachedChatData(ChatMetaDataDto? Metadata, List<ChatMessage>? Messages);