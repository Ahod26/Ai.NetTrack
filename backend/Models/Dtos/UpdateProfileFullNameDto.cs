using System.ComponentModel.DataAnnotations;

namespace backend.Models.Dtos;

public class UpdateProfileFullNameDTO
{
  [Required]
  public required string FullName { get; set; }
}