using backend.Models.Dtos;
using backend.Repository.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services.Classes;

public class EmailListCacheService(
  IRedisCacheRepo redisCacheRepo,
  ILogger<EmailListCacheService> logger) : IEmailListCacheService
{
  private const string CacheKey = "newsletter:subscribers";

  public async Task ToggleUserFromNewsletterAsync(EmailNewsletterDTO newUser)
  {
    try
    {
      var isSubscribed = await redisCacheRepo.IsUserInNewsletterListAsync(CacheKey, newUser.Email);

      if (isSubscribed)
      {
        await redisCacheRepo.RemoveUserFromNewsletterListAsync(CacheKey, newUser.Email);
      }

      await redisCacheRepo.AddUserToNewsletterListAsync(CacheKey, newUser);

      logger.LogInformation("Added user {Email} to newsletter subscribers.", newUser.Email);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error adding user to newsletter");
      throw;
    }
  }

  public async Task RemoveUserFromNewsletterAsync(string email)
  {
    try
    {
      await redisCacheRepo.RemoveUserFromNewsletterListAsync(CacheKey, email);

      logger.LogInformation("Removed user {Email} from newsletter subscribers.", email);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error removing user from newsletter");
      throw;
    }
  }

  public async Task<List<EmailNewsletterDTO>> GetNewsletterRecipients()
  {
    try
    {
      return await redisCacheRepo.GetNewsletterSubscribersListAsync(CacheKey);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting newsletter list");
      return [];
    }
  }

  public async Task UpdateUserInfo(string email, string fullName)
  {
    await RemoveUserFromNewsletterAsync(email);
    await ToggleUserFromNewsletterAsync(new EmailNewsletterDTO ( Email: email, FullName: fullName ));
  }
}