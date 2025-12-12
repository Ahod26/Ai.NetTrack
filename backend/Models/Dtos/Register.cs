using System.ComponentModel.DataAnnotations;
namespace backend.Models.Dtos;

public sealed record RegisterDTO(
  [Required][EmailAddress] string Email,
  [Required] string FullName,
  [Required] string Password,
  [Required] bool IsSubscribedToNewsletter);