namespace backend.Models.Dtos;

public class ApiUserDto
{
  public string FullName { get; set; } = "";
  public string Email { get; set; } = "";
  public bool IsSubscribedToNewsletter { get; set; } = false;
}