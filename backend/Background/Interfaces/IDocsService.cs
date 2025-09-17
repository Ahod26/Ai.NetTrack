using backend.Models.Domain;

namespace backend.Background.Interfaces;

public interface IDocsService
{
  Task<List<NewsItem>> GetMicrosoftDocsUpdatesAsync();
}