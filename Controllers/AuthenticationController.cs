using AspNet.Security.OAuth.Discord;
using Finder.Web.DataTransferObject;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Finder.Web.Controllers; 

public class AuthenticationController : Controller {
    [Route("login")]
    public IActionResult LogIn() {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" }, DiscordAuthenticationDefaults.AuthenticationScheme);
    }

    [Route("logout")]
    public async Task<IActionResult> LogOut() {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    [Route("userinfo")]
    public ActionResult<UserInfo> GetUserInfoAsync() {
        if (User.Identity is { IsAuthenticated: false }) return View("UserInfo", new UserInfo());
        var userInfo = new UserInfo { IsAuthenticated = User.Identity is { IsAuthenticated: true } };
        foreach (var claim in User.Claims) {
            switch (claim.Type) {
                case ClaimTypes.NameIdentifier:
                    userInfo.UserId = ulong.Parse(claim.Value);
                    break;
                case ClaimTypes.Name:
                    userInfo.Username = claim.Value;
                    break;
                case DiscordAuthenticationConstants.Claims.AvatarHash:
                    userInfo.AvatarHash = claim.Value;
                    break;
                case Constants.IsBotOwner:
                    userInfo.Claims.Add(claim.Type, claim.Value);
                    break;
            }
        }
        return View("UserInfo", userInfo);
    }
    
    [Route("profile")]
    public IActionResult Profile() {
        return View("Profile");
    }
}