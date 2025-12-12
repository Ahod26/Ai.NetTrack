using System.ComponentModel.DataAnnotations;

namespace backend.Models.Dtos;

public sealed record UpdateProfilePasswordDTO(
  [Required] string NewPassword, [Required] string CurrentPassword);