using Microsoft.AspNetCore.Identity;

namespace backend.Models.Domain;

public class ApiUser : IdentityUser
{
  public string FullName { get; set; } = "";
  //navigation properties
  public List<Chat> Chats { get; set; } = new();
}