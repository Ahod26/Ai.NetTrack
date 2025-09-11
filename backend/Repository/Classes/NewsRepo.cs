using backend.Data;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Repository.Classes;

public class NewsRepo(ApplicationDbContext dbContext) : INewsRepo
{
  public async Task<List<NewsItem>> GetNewsAfterAsync(int? lastId, int count)
  {
    var query = dbContext.NewsItems.OrderByDescending(n => n.Id).AsQueryable();

    if (lastId.HasValue)
    {
      query = query.Where(n => n.Id < lastId.Value); // Get items older than lastId
    }

    return await query.Take(count).ToListAsync();
  }
}