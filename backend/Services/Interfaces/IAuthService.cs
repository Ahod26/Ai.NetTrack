public interface IAuthService
{
  Task<string> LoginAsync(LoginDTO loginDTO);
  Task<bool> RegisterAsync(RegisterDTO registerDTO);
}