namespace backend.Models.Dtos;

public sealed record RegisterResponseDTO(
    bool Success,
    List<string> Errors,
    string Message = "");