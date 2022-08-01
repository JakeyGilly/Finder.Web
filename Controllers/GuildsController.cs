using Finder.Web.Models.DiscordAPIModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
namespace Finder.Web.Controllers;


public class GuildsController : Controller {
    IHttpClientFactory _httpClientFactory;
    IConfiguration _configuration;
    public GuildsController(IHttpClientFactory httpClientFactory, IConfiguration configuration) {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [Authorize]
    [Route("/guilds")]
    public async Task<IActionResult> Index() {
        var userGuilds = await DiscordApiGet("users/@me/guilds");
        if (userGuilds == null) return NotFound();
        var json = JsonConvert.DeserializeObject<List<Guild>>(userGuilds);
        return Ok(json);
        // return View();
    }
    
    
     public async Task<string?> DiscordApiGet(string urlEndpoint) {
         HttpClient client = _httpClientFactory.CreateClient();
        var accessToken = await HttpContext.GetTokenAsync("access_token");
        var tokenType = await HttpContext.GetTokenAsync("token_type") ?? "Bearer";
        var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
        if (accessToken == null || refreshToken == null) return null;
        client.DefaultRequestHeaders.Add("Authorization", $"{tokenType} {accessToken}");
        var response = await client.GetAsync($"https://discord.com/api/{urlEndpoint}");
        if (response.StatusCode == HttpStatusCode.Unauthorized) {
            var refreshTokenClient = _httpClientFactory.CreateClient();
            var requestData = new Dictionary<string, string> {
                ["grant_type"] = "refresh_token", 
                ["refresh_token"] = refreshToken,
                ["client_id"] = _configuration.GetSection("Discord").GetValue<string>("ClientId"),
                ["client_secret"] = _configuration.GetSection("Discord").GetValue<string>("ClientSecret")
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token") {
                Content = new FormUrlEncodedContent(requestData)
            };
            var refreshResponse = await refreshTokenClient.SendAsync(request);
            var responseString = await refreshResponse.Content.ReadAsStringAsync();
            var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            var newAccessToken = responseData.GetValueOrDefault("access_token");
            var newRefreshToken = responseData.GetValueOrDefault("refresh_token");
            if (newAccessToken == null || newRefreshToken == null) return null;
            var authInfo = await HttpContext.AuthenticateAsync();
            authInfo.Properties.UpdateTokenValue("access_token", newAccessToken);
            authInfo.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
            await HttpContext.SignInAsync(authInfo.Principal, authInfo.Properties);
            var newClient = _httpClientFactory.CreateClient();
            newClient.DefaultRequestHeaders.Add("Authorization", $"{tokenType} {newAccessToken}");
            response = await newClient.GetAsync($"https://discord.com/api/{urlEndpoint}");
        }
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }
}