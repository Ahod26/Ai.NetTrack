namespace backend.Models.Dtos;

public class UserInfoDTO
{
  public List<string> Roles { get; set; } = [];
  public ApiUserDto? ApiUserDto { get; set; }
}
