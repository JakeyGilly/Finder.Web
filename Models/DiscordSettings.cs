namespace Finder.Web.Models;

public class DiscordSettings {
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string BotToken { get; set; }
    public List<Int64> OwnerIds { get; set; }
}