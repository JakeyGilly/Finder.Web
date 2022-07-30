namespace Finder.Web.DataTransferObject;

public class UserInfo {
    public bool IsAuthenticated { get; set; }
    public string Username { get; set; }
    public ulong UserId { get; set; }
    public string AvatarHash { get; set; }
    public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();
}
