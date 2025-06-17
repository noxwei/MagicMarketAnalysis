using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MagicMarketAnalysis.Pages;

public class PresetsModel : PageModel
{
    private readonly ILogger<PresetsModel> _logger;

    public PresetsModel(ILogger<PresetsModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        // Static preset page - no server-side logic needed
        // All presets are built into the view with direct links to screener
    }
}