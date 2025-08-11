using System.ComponentModel.DataAnnotations;

public class RegisterDTO
{
  [Required]
  [EmailAddress]
  public required string Email { get; set; }
  
  [Required]
  public required string UserName { get; set; }

  [Required]
  public required string Password { get; set; }
}