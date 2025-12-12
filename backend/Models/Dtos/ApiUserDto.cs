namespace backend.Models.Dtos;

public sealed record ApiUserDto(
  string FullName = "",
  string Email = "",
  bool IsSubscribedToNewsletter = true);