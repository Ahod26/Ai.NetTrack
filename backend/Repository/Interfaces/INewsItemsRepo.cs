using backend.Models.Domain;

namespace backend.Repository.Interfaces;

public interface INewsItemRepo
{
  Task AddItems(List<NewsItem> items);
  Task<List<NewsItem>> GetNewsAsync(List<DateTime> targetDates, int newsType);
  Task<List<NewsItem>> GetNewsBySearchAsync(string term);
}