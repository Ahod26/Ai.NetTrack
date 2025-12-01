namespace backend.Models.Dtos;

public class EmailNewsletterDTO
{
  public required string Email { get; set; }
  public string FullName { get; set; } = "";
}