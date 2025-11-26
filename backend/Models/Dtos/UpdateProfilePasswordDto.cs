using System.ComponentModel.DataAnnotations;

namespace backend.Models.Dtos;

public class UpdateProfilePasswordDTO
{
  [Required]
  public required string NewPassword { get; set; }

  [Required]
  public required string CurrentPassword { get; set; }
}