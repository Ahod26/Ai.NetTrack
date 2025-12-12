using System.ComponentModel.DataAnnotations;

namespace backend.Models.Dtos;

public sealed record UpdateProfileFullNameDTO([Required] string FullName);