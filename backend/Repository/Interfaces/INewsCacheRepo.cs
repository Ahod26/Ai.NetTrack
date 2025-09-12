using backend.Models.Domain;

namespace backend.Repository.Interfaces;

public interface INewsCacheRepo
{
  Task<List<NewsItem>?> GetNewsByDateAsync(string dateKey);
  Task SetNewsByDateAsync(string dateKey, List<NewsItem> news);
  Task ClearAllNewsCacheAsync();
}