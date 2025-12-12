namespace backend.Models.Dtos;

public sealed record LoginResponseDTO(
    bool Success,
    UserInfoDTO? UserInfo,
    string Message = "");