using Microsoft.AspNetCore.Identity;

public class ApiUser : IdentityUser
{
  //navigation properties
  public List<Chat> Chats { get; set; } = new();
}