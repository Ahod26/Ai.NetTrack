using Microsoft.AspNetCore.Identity;

namespace backend.Models.Domain;

public class ApiUser : IdentityUser
{
  //navigation properties
  public List<Chat> Chats { get; set; } = new();
}