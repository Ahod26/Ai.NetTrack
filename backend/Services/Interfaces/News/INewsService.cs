using backend.Models.Domain;

namespace backend.Services.Interfaces.News;

public interface INewsService
{
  Task<List<NewsItem>> GetNewsItemsAsync(List<DateTime> targetDates, int newsType);
  Task<List<NewsItem>> GetNewsItemsBySearchAsync(string term);
  Task<string?> GetContentForRelatedNewsAsync(string relatedNewsURL);
}