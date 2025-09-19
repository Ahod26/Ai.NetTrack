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
  [HttpGet("{newsType:int:range(1,4)}")]
  public async Task<IActionResult> GetNewsByDate([FromRoute] int? newsType, [FromQuery] List<DateTime>? dates = null)
  {
    try
    {
      var targetDates = dates?.Select(d => d.Date).ToList() ?? new List<DateTime> { DateTime.UtcNow.Date };

      if (targetDates.Count > 5)
        return BadRequest("Too many dates requested");

      var allNews = new List<NewsItem>();

      foreach (var date in targetDates)
      {
        var newsForDate = await newsService.GetNewsItems(date, newsType.HasValue ? newsType.Value : 0);
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