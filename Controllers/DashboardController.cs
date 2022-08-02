using Finder.Web.Models;
using Finder.Web.Models.DiscordAPIModels;
using Finder.Web.Models.DTO;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Finder.Web.Controllers;

[Authorize]
[Route("[controller]")]
public class DashboardController : Controller {
    private readonly ILogger<DashboardController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DiscordSettings _discordSettings;
    public DashboardController(ILogger<DashboardController> logger, IHttpClientFactory httpClientFactory, DiscordSettings discordSettings) {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _discordSettings = discordSettings;
    }
    
    [Route("")]
    public async Task<IActionResult> Index() {
        return View("Index", new DashboardSelectorDTO() {
            BotGuilds =  JsonConvert.DeserializeObject<List<Guild>>(await (await AccessTokenRefreshWrapper(async () => await BotDiscordApiGet("users/@me/guilds"))).Content.ReadAsStringAsync()),
            UserGuilds =  JsonConvert.DeserializeObject<List<Guild>>(await (await AccessTokenRefreshWrapper(async () => await UserDiscordApiGet("users/@me/guilds"))).Content.ReadAsStringAsync()),
            UserProfile =  JsonConvert.DeserializeObject<User>(await (await AccessTokenRefreshWrapper(async () => await UserDiscordApiGet("users/@me"))).Content.ReadAsStringAsync())
        });
    }
    
    
    [Route("{id}")]
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
        client.DefaultRequestHeaders.Add("Authorization", $"Bot {_discordSettings.BotToken}");
        return await client.GetAsync($"https://discord.com/api/{urlEndpoint}");
    }
    [NonAction]
    private async Task RefreshAccessToken(string refreshToken) {
        var client = _httpClientFactory.CreateClient();
        var requestData = new Dictionary<string, string> {
            ["grant_type"] = "refresh_token", 
            ["refresh_token"] = refreshToken,
            ["client_id"] = _discordSettings.ClientId,
            ["client_secret"] = _discordSettings.ClientSecret
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token") {
            Content = new FormUrlEncodedContent(requestData)
        };
        var response = await client.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
        if (responseData != null) {
            var newAccessToken = responseData.GetValueOrDefault("access_token");
            var newRefreshToken = responseData.GetValueOrDefault("refresh_token");
            var authInfo = await HttpContext.AuthenticateAsync();
            if (authInfo.Properties != null) {
                if (newAccessToken != null) authInfo.Properties.UpdateTokenValue("access_token", newAccessToken);
                if (newRefreshToken != null) authInfo.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
                if (authInfo.Principal != null) await HttpContext.SignInAsync(authInfo.Principal, authInfo.Properties);
            }
        }
    }
}