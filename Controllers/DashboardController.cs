using Finder.Web.Models;
using Finder.Web.Models.DiscordAPIModels;
using Finder.Web.Models.DTO;
using Finder.Web.Repositories.Bot;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Finder.Web.Controllers;

[Authorize]
[Route("dashboard")]
public class DashboardController : Controller {
    private readonly ILogger<DashboardController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DiscordSettings _discordSettings;
    private readonly AddonsRepository _addonsRepository;
    public DashboardController(ILogger<DashboardController> logger, IHttpClientFactory httpClientFactory, DiscordSettings discordSettings, AddonsRepository addonsRepository) {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _discordSettings = discordSettings;
        _addonsRepository = addonsRepository;
    }
    
    [Route("")]
    public async Task<IActionResult> Index() {
        return View("Index", new DashboardSelectorDTO {
            BotGuilds =  JsonConvert.DeserializeObject<List<Guild>>(await (await AccessTokenRefreshWrapper(async () => await BotDiscordApiGet("users/@me/guilds"))).Content.ReadAsStringAsync()),
            UserGuilds =  JsonConvert.DeserializeObject<List<Guild>>(await (await AccessTokenRefreshWrapper(async () => await UserDiscordApiGet("users/@me/guilds"))).Content.ReadAsStringAsync()),
            UserProfile =  JsonConvert.DeserializeObject<User>(await (await AccessTokenRefreshWrapper(async () => await UserDiscordApiGet("users/@me"))).Content.ReadAsStringAsync())
        });
    }
    
    
    [Route("{id}")]
    public async Task<IActionResult> Guild(string id) {
        return View("Dashboard", new GuildDashboardDTO {
            Guild = JsonConvert.DeserializeObject<Guild>(await (await AccessTokenRefreshWrapper(async () => await BotDiscordApiGet($"guilds/{id}", new Dictionary<string, string> {{"with_counts", "true"}}))).Content.ReadAsStringAsync()),
            GuildMembers = JsonConvert.DeserializeObject<List<GuildMember>>(
                await (await AccessTokenRefreshWrapper(async () => 
                    await BotDiscordApiGet($"guilds/{id}/members", new Dictionary<string, string> {{ "limit", "1000" }})
                )).Content.ReadAsStringAsync()),
        });
    }
    
    [HttpPost("{id}")]
    public async Task<IActionResult> GuildUpdate(string id, [FromForm] string ticTacToeAddon, [FromForm] string economyAddon, [FromForm] string levelingAddon, [FromForm] string ticketingAddon) {
        var guildId = ulong.Parse(id);
        await _addonsRepository.AddAddonAsync(guildId, "TicTacToe", ticTacToeAddon);
        await _addonsRepository.AddAddonAsync(guildId, "Economy", economyAddon);
        await _addonsRepository.AddAddonAsync(guildId, "Leveling", levelingAddon);
        await _addonsRepository.AddAddonAsync(guildId, "Ticketing", ticketingAddon);
        await _addonsRepository.SaveAsync();
        return RedirectToAction("Index");
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
    private async Task<HttpResponseMessage> UserDiscordApiGet(string urlEndpoint, Dictionary<string, string>? queryParams = null) {
        var client = _httpClientFactory.CreateClient();
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        if (queryParams == null) return await client.GetAsync($"https://discord.com/api/{urlEndpoint}");
        return await client.GetAsync($"https://discord.com/api/{urlEndpoint}?{string.Join("&", queryParams.Select(x => $"{x.Key}={x.Value}"))}");
    }
    [NonAction]
    private async Task<HttpResponseMessage> BotDiscordApiGet(string urlEndpoint, Dictionary<string, string>? queryParams = null) {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bot {_discordSettings.BotToken}");
        if (queryParams == null) return await client.GetAsync($"https://discord.com/api/{urlEndpoint}");
        return await client.GetAsync($"https://discord.com/api/{urlEndpoint}?{string.Join("&", queryParams.Select(x => $"{x.Key}={x.Value}"))}");
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