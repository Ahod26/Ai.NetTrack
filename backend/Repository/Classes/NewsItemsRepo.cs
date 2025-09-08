using backend.Data;
using backend.Models.Domain;
using backend.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Repository.Classes;

public class NewsItemsRepo(ApplicationDbContext dbContext) : INewsItemRepo
{
  public async Task AddItems(List<NewsItem> items)
  {
    if (items.Count == 0) return;

    // Take all of the URLs
    var urls = items.Where(i => !string.IsNullOrEmpty(i.Url)).Select(i => i.Url).ToList();

    // Find existing URLs
    var existingUrls = await dbContext.NewsItems
      .Where(n => urls.Contains(n.Url))
      .Select(n => n.Url)
      .ToListAsync();

    // Filter out items 
    var newItems = items.Where(item =>
      string.IsNullOrEmpty(item.Url) || !existingUrls.Contains(item.Url)).ToList();

    if (newItems.Count != 0)
    {
      await dbContext.NewsItems.AddRangeAsync(newItems);
      await dbContext.SaveChangesAsync();
    }
  }
}