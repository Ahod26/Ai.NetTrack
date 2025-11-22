namespace backend.Models.Dtos;

public class UserInfoDTO
{
  public string FullName { get; set; } = "";
  public string Email { get; set; } = "";
  public List<string> Roles { get; set; } = [];
}
