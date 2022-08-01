using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Finder.Web.Controllers;

[Authorize]
[Route("/dashboard")]
public class DashboardController : Controller {
    IHttpClientFactory _httpClientFactory;
    IConfiguration _configuration;
    public DashboardController(IHttpClientFactory httpClientFactory, IConfiguration configuration) {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }
    
    public async Task<IActionResult> Index() {
        var response =
            await AccessTokenRefreshWrapper(async () => await BotDiscordApiGet("users/@me/guilds"));
        var response2 =
            await AccessTokenRefreshWrapper(async () => await UserDiscordApiGet("users/@me/guilds"));
        var response3 =
            await AccessTokenRefreshWrapper(async () => await UserDiscordApiGet("users/@me"));
        ViewBag.Responces = new List<HttpResponseMessage> { response, response2, response3 };
        ViewBag.DarkMode = true;
        return View("Index");
    }
    
    
    [Route("/dashboard/{id}")]
    public IActionResult Guild(string id) {
        // return View()
        return NoContent();
    }
    
    [NonAction]
    private async Task<HttpResponseMessage> AccessTokenRefreshWrapper(Func<Task<HttpResponseMessage>> initialRequest) {
        var response = await initialRequest();
        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
        if (refreshToken == null) return response;
        await RefreshAccessToken(refreshToken);
        response = await initialRequest();
        return response;
    }
    [NonAction]
    private async Task<HttpResponseMessage> UserDiscordApiGet(string urlEndpoint) {
        var client = _httpClientFactory.CreateClient();
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        return await client.GetAsync($"https://discord.com/api/{urlEndpoint}");
    }
    [NonAction]
    private async Task<HttpResponseMessage> BotDiscordApiGet(string urlEndpoint) {
        var client = _httpClientFactory.CreateClient();
        var botToken = _configuration.GetSection("Discord").GetValue<string>("BotToken");
        client.DefaultRequestHeaders.Add("Authorization", $"Bot {botToken}");
        return await client.GetAsync($"https://discord.com/api/{urlEndpoint}");
    }
    [NonAction]
    private async Task RefreshAccessToken(string refreshToken) {
        var client = _httpClientFactory.CreateClient();
        var requestData = new Dictionary<string, string> {
            ["grant_type"] = "refresh_token", 
            ["refresh_token"] = refreshToken,
            ["client_id"] = _configuration.GetSection("Discord").GetValue<string>("ClientId"),
            ["client_secret"] = _configuration.GetSection("Discord").GetValue<string>("ClientSecret")
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token") {
            Content = new FormUrlEncodedContent(requestData)
        };
        var response = await client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
        var newAccessToken = responseData.GetValueOrDefault("access_token");
        var newRefreshToken = responseData.GetValueOrDefault("refresh_token");
        var authInfo = await HttpContext.AuthenticateAsync();
        authInfo.Properties.UpdateTokenValue("access_token", newAccessToken);
        authInfo.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
        await HttpContext.SignInAsync(authInfo.Principal, authInfo.Properties);
    }
}