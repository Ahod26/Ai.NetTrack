using System.ComponentModel.DataAnnotations;

namespace backend.Models.Dtos;

public sealed record UpdateProfileEmailDTO([Required][EmailAddress] string Email);