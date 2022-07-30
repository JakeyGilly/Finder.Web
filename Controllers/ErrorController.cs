using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Finder.Web.Controllers;

public class ErrorController : Controller {
    private readonly ILogger<ErrorController> _logger;
    public ErrorController(ILogger<ErrorController> logger) {
        _logger = logger;
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index() {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.Log(LogLevel.Critical, "Error");
        return View("Error", new ErrorViewModel() {
            RequestId = requestId
        });
    }
}

public class ErrorViewModel {
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}