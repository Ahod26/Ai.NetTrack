using backend.Models.Domain;

namespace backend.Services.Interfaces.NewsAggregation;

public interface IDocsService
{
  Task<List<NewsItem>> GetMicrosoftDocsUpdatesAsync();
}