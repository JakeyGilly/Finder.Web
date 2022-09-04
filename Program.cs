using Finder.Web.Database;
using Finder.Web.Models;
using Finder.Web.Repositories.Bot;
using Finder.Web.Repositories.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Finder.Web;

public static class Program {
    public static void Main(string[] args) {
        var builder = WebApplication.CreateBuilder(args);
        DiscordSettings discordSettings = builder.Configuration.GetSection("Discord").Get<DiscordSettings>();
        ConnectionSettings connectionSettings = builder.Configuration.GetSection("ConnectionStrings").Get<ConnectionSettings>();
        builder.Services.AddDbContext<ApplicationContext>(options =>
            options.UseNpgsql(connectionSettings.Default, options2 => options2.MigrationsAssembly("Finder.Database")),
            ServiceLifetime.Transient
        );
        builder.Services.AddSingleton(discordSettings);
        builder.Services.AddSingleton(connectionSettings);
        builder.Services.AddScoped<UserSettingsRepository>();
        builder.Services.AddScoped<AddonsRepository>();
        builder.Services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }).AddCookie(options => {
            options.LoginPath = "/signin";
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
        }).AddDiscord(options => {
            options.Scope.Add("identify");
            options.Scope.Add("guilds");
            options.Prompt = "none";
            options.ClientId = discordSettings.ClientId;
            options.ClientSecret = discordSettings.ClientSecret;
            options.SaveTokens = true;
            options.Events = new OAuthEvents {
                OnCreatingTicket = context => {
                    if (!ulong.TryParse(context.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Task.CompletedTask;
                    if (discordSettings.OwnerIds.IsNullOrEmpty() || !discordSettings.OwnerIds.Contains((long)userId)) {
                        context.Identity.AddClaim(new Claim("IsBotOwner", "false"));
                    } else {
                        context.Identity.AddClaim(new Claim("IsBotOwner", "true"));
                    }
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
