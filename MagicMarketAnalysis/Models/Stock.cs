namespace MagicMarketAnalysis.Models;

public class Stock
{
    public required string Symbol { get; set; }
    public required string CompanyName { get; set; }
    public decimal Price { get; set; }
    public decimal? PERatio { get; set; }
    public decimal? RSI { get; set; }
    public long Volume { get; set; }
    public decimal? MarketCap { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePercent { get; set; }
    public DateTime LastUpdated { get; set; }
}