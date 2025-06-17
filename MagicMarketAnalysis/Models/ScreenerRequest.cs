namespace MagicMarketAnalysis.Models;

public class ScreenerRequest
{
    public decimal? MinPE { get; set; }
    public decimal? MaxPE { get; set; }
    public decimal? MinRSI { get; set; }
    public decimal? MaxRSI { get; set; }
    public long? MinVolume { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? MinMarketCap { get; set; }
    public decimal? MaxMarketCap { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
    public string SortBy { get; set; } = "Symbol";
    public bool SortDescending { get; set; } = false;
}