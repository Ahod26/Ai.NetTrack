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

  public async Task<List<NewsItem>> GetNewsAsync(List<DateTime> targetDates, int newsType)
  {
    var dates = targetDates.Select(d => d.Date).ToList();

    IQueryable<NewsItem> query = dbContext.NewsItems.AsNoTracking();

    query = query.Where(n => dates.Any(d =>
      n.PublishedDate >= d && n.PublishedDate < d.AddDays(1)));

    if (newsType != 0)
    {
      query = query.Where(n => (int)n.SourceType == newsType);
    }

    return await query
        .OrderByDescending(n => n.PublishedDate)
        .ToListAsync();
  }

  public async Task<List<NewsItem>> GetNewsBySearchAsync(string term)
  {
    var words = term.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    IQueryable<NewsItem> query = dbContext.NewsItems.AsNoTracking();

    // Each word must exist somewhere in title OR summary
    foreach (var word in words)
    {
      query = query.Where(n =>
        n.Summary!.ToLower().Contains(word) ||
        n.Title.ToLower().Contains(word));
    }

    return await query.ToListAsync();
  }
}