using AspNetCoreRateLimit;
using Finder.Database.DatabaseContexts;
using Finder.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var discordSettings = builder.Configuration.GetSection("Discord").Get<DiscordSettings>();
var connectionSettings = builder.Configuration.GetSection("ConnectionStrings").Get<ConnectionSettings>();
builder.Services.AddDbContext<ApplicationContext>(
    options => options.UseNpgsql(
        connectionSettings.Default,
        optionsBuilder => optionsBuilder.MigrationsAssembly("Finder.Database")
    )
);
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options => {
    options.LoginPath = "/signin";
    options.ExpireTimeSpan = new TimeSpan(7, 0, 0, 0);
}).AddDiscord(options => {
    options.ClientId = discordSettings.ClientId;
    options.ClientSecret = discordSettings.ClientSecret;
    options.Events = new OAuthEvents {
        OnCreatingTicket = ticketContext => {
            if (!ulong.TryParse(ticketContext.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Task.CompletedTask;
            foreach (var ownerId in discordSettings.OwnerIds.Where(ownerId => userId == (ulong)ownerId)) {
                ticketContext.Principal.AddIdentity(new ClaimsIdentity(new [] { new Claim(Constants.IsBotOwner, "true") }));
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization(options => {
    options.AddPolicy(Constants.IsBotOwner, policy => policy.RequireClaim(Constants.IsBotOwner, "true"));
});
builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
builder.Services.Configure<ClientRateLimitPolicies>(builder.Configuration.GetSection("ClientRateLimitPolicies"));
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddControllersWithViews();

var app = builder.Build();
if (!app.Environment.IsDevelopment()) {
    app.UseExceptionHandler("/Error");
    app.UseHsts();
} else {
    app.UseDeveloperExceptionPage();
}
app.UseForwardedHeaders(new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseClientRateLimiting();
app.UseIpRateLimiting();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();