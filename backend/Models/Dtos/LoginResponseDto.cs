namespace backend.Models.Dtos;

public class LoginResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public UserInfoDTO? UserInfo { get; set; }
}