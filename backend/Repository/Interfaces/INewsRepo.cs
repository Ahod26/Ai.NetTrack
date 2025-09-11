using backend.Models.Domain;

namespace backend.Repository.Interfaces;

public interface INewsRepo
{
  Task<List<NewsItem>> GetNewsAfterAsync(int? lastId, int count);
}