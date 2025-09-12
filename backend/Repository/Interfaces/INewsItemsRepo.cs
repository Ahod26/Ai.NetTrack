using backend.Models.Domain;

namespace backend.Repository.Interfaces;

public interface INewsItemRepo
{
  Task AddItems(List<NewsItem> items);
  Task<List<NewsItem>> GetNewsByDateAsync(DateTime targetDate);
}