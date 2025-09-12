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
  public async Task<IActionResult> GetNewsPerDate([FromQuery] DateTime? date = null)
  {
    try
    {
      var targetDate = date?.Date ?? DateTime.UtcNow.Date;
      var news = await newsService.GetNewsItems(targetDate);
      return Ok(news);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting news items");
      return StatusCode(500, "Error getting news items");
    }
  }
}