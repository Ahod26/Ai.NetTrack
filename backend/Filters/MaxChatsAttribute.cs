using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using backend.Repository.Interfaces;

namespace backend.Filters;

public class MaxChatsAttribute(IChatRepo chatRepo) : ActionFilterAttribute
{
  private readonly int MaxChatsPerUser = 10;
  public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
  {
    var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId != null)
    {
      int numberOfChats = await chatRepo.GetUserChatCountAsync(userId);
      if (numberOfChats + 1 > MaxChatsPerUser)
      {
        context.Result = new BadRequestObjectResult(new { error = "You've reached your chat limit of 10. Please delete an old chat to create a new one." });
        return;
      }
      await next();
      return;
    }
    context.Result = new BadRequestObjectResult(new { error = "No user found" });
  }
}