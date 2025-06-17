using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MagicMarketAnalysis.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        // Page model loads data via JavaScript/API calls
    }
}