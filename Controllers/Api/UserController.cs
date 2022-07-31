using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;

namespace Finder.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    public UserController(IHttpClientFactory httpClientFactory, IConfiguration configuration) {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }
    [Authorize]
    [HttpGet("claims")]
    public IActionResult Claims() {
        var claims = ((ClaimsIdentity)User.Identity).Claims.ToDictionary(claim => claim.Type, claim => claim.Value);
        return Ok(claims);
    }

    [Authorize]
    [HttpGet("token")]
    public async Task<IActionResult> Token() {
        return Ok(new {
            accessToken = await HttpContext.GetTokenAsync("access_token"),
            refreshToken = await HttpContext.GetTokenAsync("refresh_token"),
            tokenType = await HttpContext.GetTokenAsync("token_type")
        });
    }

    [Authorize]
    [HttpGet("discord")]
    public async Task<IActionResult> Discord() {
        var result = await DiscordApiGet("users/@me");
        return Ok(result);
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
        // var authInfo2 = await HttpContext.AuthenticateAsync();
        // authInfo2.Properties.UpdateTokenValue("access_token", "newRefreshToken");
        return content;
    }
}
    