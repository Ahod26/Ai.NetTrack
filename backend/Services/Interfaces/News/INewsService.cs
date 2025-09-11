using backend.Models.Domain;

namespace backend.Services.Interfaces.News;

public interface INewsService
{
  Task<List<NewsItem>> GetNewsItems(int? lastId, int count);
}