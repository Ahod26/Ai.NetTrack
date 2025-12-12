using backend.Models.Domain;

namespace backend.Models.Dtos;

public sealed record FullMessageDto(
  Guid Id,
  string Content,
  MessageType Type,
  int TokenCount,
  bool IsStarred,
  bool IsReported = false,
  bool IsChunkMessage = false);