using backend.Models.Domain;
using backend.Services.Interfaces.News;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
[EnableRateLimiting("news")]
public class NewsController(INewsService newsService) : ControllerBase
{
  [HttpGet("")]
  [HttpGet("{newsType:int:range(1,4)}")]
  public async Task<IActionResult> GetNewsByDate([FromRoute] int? newsType, [FromQuery] List<DateTime>? dates = null)
  {
    var targetDates = dates?.Select(d => d.Date).ToList() ?? new List<DateTime> { DateTime.UtcNow.Date };

    if (targetDates.Count > 10)
      return BadRequest("Too many dates requested");

    var allNews = await newsService.GetNewsItemsAsync(targetDates, newsType.HasValue ? newsType.Value : 0);

    // Return sorted by date, newest first
    return Ok(allNews.OrderByDescending(n => n.PublishedDate));
  }

  [HttpGet("search")]
  public async Task<IActionResult> GetNewsBySearchQuery([FromQuery] string term)
  {
    var news = await newsService.GetNewsItemsBySearchAsync(term);
    return Ok(news.OrderByDescending(n => n.PublishedDate));
  }
}