using Refit;
using MagicMarketAnalysis.Models;

namespace MagicMarketAnalysis.Services;

public interface IFmpClient
{
    Task<FmpQuoteResponse> GetQuoteAsync(string symbol);
    Task<List<FmpQuoteResponse>> GetMultipleQuotesAsync(string[] symbols);
    Task<List<FmpSectorPerformance>> GetSectorPerfAsync();
    Task<List<FmpEconomicEvent>> GetEconomicCalendarAsync(DateTime? from = null, DateTime? to = null);
    Task<FmpCompanyProfile?> GetCompanyProfileAsync(string symbol);
}

public class FmpQuoteResponse
{
    public required string Symbol { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public decimal ChangesPercentage { get; set; }
    public decimal Change { get; set; }
    public decimal DayLow { get; set; }
    public decimal DayHigh { get; set; }
    public decimal YearHigh { get; set; }
    public decimal YearLow { get; set; }
    public long MarketCap { get; set; }
    public decimal? PriceAvg50 { get; set; }
    public decimal? PriceAvg200 { get; set; }
    public string? Exchange { get; set; }
    public long Volume { get; set; }
    public long AvgVolume { get; set; }
    public string? EarningsAnnouncement { get; set; }
    public decimal? Pe { get; set; }
    public decimal? Eps { get; set; }
    public int Timestamp { get; set; }
}

public class FmpSectorPerformance
{
    public required string Sector { get; set; }
    public required string ChangesPercentage { get; set; }
}

public class FmpEconomicEvent
{
    public required string Event { get; set; }
    public required string Date { get; set; }
    public required string Country { get; set; }
    public string? Actual { get; set; }
    public string? Previous { get; set; }
    public string? Change { get; set; }
    public string? ChangePercentage { get; set; }
    public string? Estimate { get; set; }
    public string? Impact { get; set; }
}

public class FmpCompanyProfile
{
    public required string Symbol { get; set; }
    public decimal Price { get; set; }
    public decimal? Beta { get; set; }
    public long VolAvg { get; set; }
    public long MktCap { get; set; }
    public decimal? LastDiv { get; set; }
    public string? Range { get; set; }
    public decimal? Changes { get; set; }
    public required string CompanyName { get; set; }
    public string? Currency { get; set; }
    public string? Cik { get; set; }
    public string? Isin { get; set; }
    public string? Cusip { get; set; }
    public string? Exchange { get; set; }
    public string? ExchangeShortName { get; set; }
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string? Ceo { get; set; }
    public string? Sector { get; set; }
    public string? Country { get; set; }
    public string? FullTimeEmployees { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public decimal? DcfDiff { get; set; }
    public decimal? Dcf { get; set; }
    public string? Image { get; set; }
    public string? IpoDate { get; set; }
    public bool DefaultImage { get; set; }
    public bool IsEtf { get; set; }
    public bool IsActivelyTrading { get; set; }
    public bool IsAdr { get; set; }
    public bool IsFund { get; set; }
}

public class FmpMarketHours
{
    public required string StockExchangeName { get; set; }
    public required string StockMarketHours { get; set; }
    public required string StockMarketHolidays { get; set; }
    public bool IsTheStockMarketOpen { get; set; }
    public bool IsTheEuronextMarketOpen { get; set; }
    public bool IsTheForexMarketOpen { get; set; }
    public bool IsTheCryptoMarketOpen { get; set; }
}