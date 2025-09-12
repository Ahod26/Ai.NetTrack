using backend.Models.Domain;

namespace backend.Services.Interfaces.News;

public interface INewsService
{
  Task<List<NewsItem>> GetNewsItems(DateTime targetDate);
}