using Finder.Web.Models.DiscordAPIModels;

namespace Finder.Web.Models.DTO;

public class DashboardSelectorDTO {
    public List<Guild>? BotGuilds { get; set; } = new List<Guild>();
    public List<Guild>? UserGuilds { get; set; } = new List<Guild>();
    public User? UserProfile { get; set; } = new User();
}