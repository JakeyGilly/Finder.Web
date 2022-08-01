using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;

namespace Finder.Web;

public static class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        DiscordSettings discordSettings = builder.Configuration.GetSection("Discord").Get<DiscordSettings>();
        builder.Services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(options => {
            options.LoginPath = "/signin";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
        }).AddDiscord(options => {
            options.Scope.Add("identify");
            options.Scope.Add("guilds");
            options.ClientId = discordSettings.ClientId;
            options.ClientSecret = discordSettings.ClientSecret;
            options.SaveTokens = true;
            options.Events = new OAuthEvents {
                OnCreatingTicket = context => {
                    if (!ulong.TryParse(context.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Task.CompletedTask;
                    context.Identity.AddClaim(discordSettings.OwnerIds.Contains((long)userId) 
                        ? new Claim("IsBotOwner", "true") : new Claim("IsBotOwner", "false"));
                    return Task.CompletedTask;
                }
            };
        });
        builder.Services.AddAuthorization(options => {
            options.AddPolicy("IsBotOwner", policy => policy.RequireClaim("IsBotOwner", "true", "false"));
        });
        builder.Services.AddHttpClient();
        builder.Services.AddControllersWithViews();
        var app = builder.Build();
        if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();
        app.UseRouting();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
        app.Run();
    }
}
