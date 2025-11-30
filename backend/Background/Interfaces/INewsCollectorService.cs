namespace backend.Background.Interfaces;

public interface INewsCollectorService
{
  Task<int> CollectAllNews();
}