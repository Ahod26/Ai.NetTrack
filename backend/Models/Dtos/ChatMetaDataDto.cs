namespace backend.Models.Dtos;

public sealed record ChatMetaDataDto(
  Guid Id,
  DateTime CreatedAt,
  DateTime LastMessageAt,
  bool IsContextFull,
  string Title = "New Chat",
  int MessageCount = 0);