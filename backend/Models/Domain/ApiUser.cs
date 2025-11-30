using Microsoft.AspNetCore.Identity;

namespace backend.Models.Domain;

public class ApiUser : IdentityUser
{
  public string FullName { get; set; } = "";
  public bool IsSubscribedToNewsletter { get; set; } = false;
  
  //navigation properties
  public List<Chat> Chats { get; set; } = new();
}