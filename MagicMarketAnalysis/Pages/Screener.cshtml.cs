using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MagicMarketAnalysis.Data;
using MagicMarketAnalysis.Models;

namespace MagicMarketAnalysis.Pages;

public class ScreenerModel : PageModel
{
    private readonly ILogger<ScreenerModel> _logger;
    private readonly IStockRepository _stockRepository;

    public ScreenerModel(ILogger<ScreenerModel> logger, IStockRepository stockRepository)
    {
        _logger = logger;
        _stockRepository = stockRepository;
    }

    [BindProperty(SupportsGet = true)]
    public decimal? MinPE { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPE { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MaxPrice { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? MinMarketCap { get; set; }

    [BindProperty(SupportsGet = true)]
    public long? MinVolume { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sector { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "Volume";

    [BindProperty(SupportsGet = true)]
    public bool SortDescending { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public new int Page { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    [BindProperty(SupportsGet = true)]
    public string? Preset { get; set; }

    public List<Stock> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public async Task OnGetAsync()
    {
        // Apply preset if specified
        if (!string.IsNullOrEmpty(Preset))
        {
            ApplyPreset(Preset);
        }

        // Only search if we have any criteria set
        if (HasSearchCriteria())
        {
            await SearchStocksAsync();
        }
    }

    private bool HasSearchCriteria()
    {
        return MinPE.HasValue || MaxPE.HasValue || MinPrice.HasValue || MaxPrice.HasValue ||
               MinMarketCap.HasValue || MinVolume.HasValue || !string.IsNullOrEmpty(Sector);
    }

    private async Task SearchStocksAsync()
    {
        try
        {
            var request = new ScreenerRequest
            {
                MinPE = MinPE,
                MaxPE = MaxPE,
                MinPrice = MinPrice,
                MaxPrice = MaxPrice,
                MinMarketCap = MinMarketCap,
                MinVolume = MinVolume,
                Sector = Sector,
                SortBy = SortBy,
                SortDescending = SortDescending,
                PageNumber = Page,
                PageSize = PageSize
            };

            var result = await _stockRepository.SearchAsync(request);
            Results = result.Stocks;
            TotalCount = result.TotalCount;
            TotalPages = result.TotalPages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching stocks with criteria");
            Results = new List<Stock>();
            TotalCount = 0;
            TotalPages = 0;
        }
    }

    private void ApplyPreset(string presetName)
    {
        switch (presetName.ToLower())
        {
            case "value":
                MinPE = 5;
                MaxPE = 15;
                MinMarketCap = 1_000_000_000m; // $1B+
                SortBy = "PERatio";
                SortDescending = false;
                break;

            case "tech":
                Sector = "Technology";
                MinMarketCap = 10_000_000_000m; // $10B+
                SortBy = "MarketCap";
                SortDescending = true;
                break;

            case "volume":
                MinVolume = 10_000_000; // 10M+ volume
                SortBy = "Volume";
                SortDescending = true;
                break;

            case "growth":
                MinMarketCap = 2_000_000_000m; // $2B+
                MinVolume = 1_000_000; // 1M+ volume
                SortBy = "MarketCap";
                SortDescending = true;
                break;

            case "large-cap":
                MinMarketCap = 200_000_000_000m; // $200B+
                SortBy = "MarketCap";
                SortDescending = true;
                break;
        }
    }
}