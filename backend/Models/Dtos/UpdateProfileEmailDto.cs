using System.ComponentModel.DataAnnotations;

namespace backend.Models.Dtos;

public class UpdateProfileEmailDTO
{
  [Required]
  [EmailAddress]
  public required string Email { get; set; }
}