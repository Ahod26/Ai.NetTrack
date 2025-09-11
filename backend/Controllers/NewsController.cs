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
  public async Task<IActionResult> GetNews([FromQuery] int? lastId = null)
  {
    try
    {
      var news = await newsService.GetNewsItems(lastId, 10);
      return Ok(news);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error getting news items");
      return StatusCode(500, "Error getting news items");
    }
  }
}