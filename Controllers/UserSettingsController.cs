using Finder.Web.Repositories.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Finder.Web.Controllers;

[Authorize]
[Route("user/settings")]
public class UserSettingsController : Controller {
    private readonly ILogger<UserSettingsController> _logger;
    private readonly UserSettingsRepository _userSettingsRepository;
    public UserSettingsController(ILogger<UserSettingsController> logger, UserSettingsRepository userSettingsRepository) {
        _logger = logger;
        _userSettingsRepository = userSettingsRepository;
    }
    
    [Route("")]
    public IActionResult Index() {
        return View("Index");
    }
    
    [HttpPost("")]
    public async Task<IActionResult> Update(string darkMode, string devMode) {
        var userId = ulong.Parse(User.Claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value);
        await _userSettingsRepository.AddSettingAsync(userId, "DarkMode", darkMode);
        await _userSettingsRepository.AddSettingAsync(userId, "DevMode", devMode);
        await _userSettingsRepository.SaveAsync();
        return RedirectToAction("Index");
    }
}