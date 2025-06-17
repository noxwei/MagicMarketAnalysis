using Refit;
using MagicMarketAnalysis.Models;

namespace MagicMarketAnalysis.Services;

public class FmpClient : IFmpClient
{
    private readonly IFmpClientApi _api;
    private readonly string _apiKey;
    private readonly ILogger<FmpClient> _logger;

    public FmpClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FmpClient> logger)
    {
        _apiKey = Environment.GetEnvironmentVariable("FMP_API_KEY") 
                 ?? configuration["ApiSettings:FMP:ApiKey"] 
                 ?? throw new InvalidOperationException("FMP API key not configured");
        
        _logger = logger;

        // Simplified without Polly for now - will add retry logic later

        var httpClient = httpClientFactory.CreateClient("FmpClient");
        httpClient.BaseAddress = new Uri("https://financialmodelingprep.com/api/v3/");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MagicMarketAnalysis/1.0");

        _api = RestService.For<IFmpClientApi>(httpClient);
    }

    public async Task<FmpQuoteResponse> GetQuoteAsync(string symbol)
    {
        try
        {
            _logger.LogDebug("Fetching quote for {Symbol}", symbol);
            var quotes = await _api.GetQuoteAsync(symbol, _apiKey);
            var quote = quotes.FirstOrDefault();
            
            if (quote == null)
            {
                throw new InvalidOperationException($"No quote data found for symbol {symbol}");
            }

            return quote;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch quote for {Symbol}", symbol);
            throw;
        }
    }

    public async Task<List<FmpSectorPerformance>> GetSectorPerfAsync()
    {
        try
        {
            _logger.LogDebug("Fetching sector performance data");
            var sectors = await _api.GetSectorPerfAsync(_apiKey);
            return sectors ?? new List<FmpSectorPerformance>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch sector performance");
            throw;
        }
    }

    public async Task<List<FmpEconomicEvent>> GetEconomicCalendarAsync(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            var fromDate = from?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            var toDate = to?.ToString("yyyy-MM-dd") ?? DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
            
            _logger.LogDebug("Fetching economic calendar from {FromDate} to {ToDate}", fromDate, toDate);
            var events = await _api.GetEconomicCalendarAsync(fromDate, toDate, _apiKey);
            return events ?? new List<FmpEconomicEvent>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch economic calendar");
            throw;
        }
    }

    public async Task<List<FmpQuoteResponse>> GetMultipleQuotesAsync(string[] symbols)
    {
        try
        {
            _logger.LogDebug("Fetching quotes for {Count} symbols", symbols.Length);
            var quotes = await _api.GetQuotesAsync(symbols, _apiKey);
            return quotes ?? new List<FmpQuoteResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch multiple quotes");
            throw;
        }
    }

    public async Task<FmpCompanyProfile?> GetCompanyProfileAsync(string symbol)
    {
        try
        {
            _logger.LogDebug("Fetching company profile for {Symbol}", symbol);
            var profiles = await _api.GetCompanyProfileAsync(symbol, _apiKey);
            return profiles.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch company profile for {Symbol}", symbol);
            throw;
        }
    }
}

internal interface IFmpClientApi
{
    [Get("/quote/{symbol}")]
    Task<List<FmpQuoteResponse>> GetQuoteAsync(string symbol, [Query] string apikey);

    [Get("/quote")]
    Task<List<FmpQuoteResponse>> GetQuotesAsync([Query(CollectionFormat.Csv)] string[] symbols, [Query] string apikey);

    [Get("/sectors-performance")]
    Task<List<FmpSectorPerformance>> GetSectorPerfAsync([Query] string apikey);

    [Get("/economic_calendar")]
    Task<List<FmpEconomicEvent>> GetEconomicCalendarAsync([Query] string from, [Query] string to, [Query] string apikey);

    [Get("/profile/{symbol}")]
    Task<List<FmpCompanyProfile>> GetCompanyProfileAsync(string symbol, [Query] string apikey);

    [Get("/market-hours")]
    Task<List<FmpMarketHours>> GetMarketHoursAsync([Query] string apikey);
}