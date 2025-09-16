using backend.Models.Domain;
using backend.Services.Interfaces.News;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class NewsController(INewsService newsService, ILogger<NewsController> logger) : ControllerBase
{
  [HttpGet("")]
  public async Task<IActionResult> GetNewsPerDate([FromQuery] List<DateTime>? dates = null)
  {
    try
    {
      var targetDates = dates?.Select(d => d.Date).ToList()?? new List<DateTime> { DateTime.UtcNow.Date };

      var allNews = new List<NewsItem>();

      foreach (var date in targetDates)
      {
        var newsForDate = await newsService.GetNewsItems(date);
        allNews.AddRange(newsForDate);
      }

      // Return sorted by date, newest first
      return Ok(allNews.OrderByDescending(n => n.PublishedDate));
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting news items");
      return StatusCode(500, "Error getting news items");
    }
  }
}