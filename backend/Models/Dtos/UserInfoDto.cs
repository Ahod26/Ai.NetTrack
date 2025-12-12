namespace backend.Models.Dtos;

public sealed record UserInfoDTO(ApiUserDto? ApiUserDto, List<string> Roles);