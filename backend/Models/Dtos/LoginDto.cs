using System.ComponentModel.DataAnnotations;
namespace backend.Models.Dtos;

public sealed record LoginDTO(
  [Required][EmailAddress] string Email,
  [Required] string Password);