namespace backend.Models.Dtos;

public sealed record ChunkMessageDto(string Content = "", bool IsChunkMessage = true);