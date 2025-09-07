namespace backend.Services.Interfaces.NewsAggregation;

public interface INewsCollectorService
{
  Task CollectAllNews();
}