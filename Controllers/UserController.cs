using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Finder.Web.Controllers;

[Route("api/[controller]")]
public class UserController : Controller {
    [Route("token")]
    public async Task<IActionResult> Token() {
        return Ok(new {
            access_token = await HttpContext.GetTokenAsync("access_token"),
            refresh_token = await HttpContext.GetTokenAsync("refresh_token"),
            token_type = await HttpContext.GetTokenAsync("token_type")
        });
    }
    
    [Route("claims")]
    public IActionResult Claims() {
        return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
    }
}