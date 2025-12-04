using backend.Models.Dtos;

namespace backend.Services.Interfaces;

public interface IEmailListCacheService
{
  Task ToggleUserFromNewsletterAsync(EmailNewsletterDTO newUser);
  Task<List<EmailNewsletterDTO>> GetNewsletterRecipients();
  Task RemoveUserFromNewsletterAsync(string email);
  Task UpdateUserInfo(string email, string fullName);
}