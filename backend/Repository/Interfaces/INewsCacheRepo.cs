using backend.Models.Domain;

namespace backend.Repository.Interfaces;

public interface INewsCacheRepo
{
  Task<List<NewsItem>?> GetNewsAsync(string dateKey, int newsType);
  Task SetNewsByDateAsync(string dateKey, List<NewsItem> news);
  Task ClearAllNewsCacheAsync();
}