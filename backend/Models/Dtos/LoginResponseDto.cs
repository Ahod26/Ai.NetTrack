public class LoginResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public UserInfoDTO? User { get; set; }
}