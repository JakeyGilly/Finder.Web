using Finder.Web.Database;
using Finder.Web.Models.Data.Web;
namespace Finder.Web.Repositories.Web;

public class UserSettingsRepository : Repository<UserSettingsRepository> {
    public UserSettingsRepository(ApplicationContext context) : base(context) { }

    public async Task<UserSettingsModel> GetUserSettingsModelAsync(ulong userId) {
        return await Context.Set<UserSettingsModel>().FindAsync((long)userId) ?? new UserSettingsModel();
    }

    public async Task AddSettingAsync(ulong userId, string key, string value) {
        var userSettings = await Context.Set<UserSettingsModel>().FindAsync((long)userId);
        if (userSettings == null) {
            await Context.Set<UserSettingsModel>().AddAsync(new UserSettingsModel {
                UserId = (long)userId,
                Settings = new Dictionary<string, string> {
                    { key, value }
                }
            });
            return;
        }
        if (userSettings.Settings.ContainsKey(key)) {
            userSettings.Settings[key] = value;
        } else {
            userSettings.Settings.Add(key, value);
        }
        Context.Set<UserSettingsModel>().Update(userSettings);
    }

    public async Task<bool> UserSettingExists(ulong userId, string key) {
        var settings = await Context.Set<UserSettingsModel>().FindAsync((long)userId);
        return settings != null && settings.Settings.ContainsKey(key);
    }
    
    public async Task<string?> GetUserSettingAsync(ulong userId, string key) {
        var settings = await Context.Set<UserSettingsModel>().FindAsync((long)userId);
        return settings?.Settings.GetValueOrDefault(key);
    }
}