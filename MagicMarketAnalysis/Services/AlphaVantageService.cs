using MagicMarketAnalysis.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace MagicMarketAnalysis.Services;

public class AlphaVantageService : IMarketDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlphaVantageService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly int _rateLimitPerMinute;
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly List<DateTime> _requestTimes = new();

    public AlphaVantageService(
        HttpClient httpClient, 
        IMemoryCache cache, 
        IConfiguration configuration,
        ILogger<AlphaVantageService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        
        _apiKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY") ?? 
                 _configuration["ApiSettings:AlphaVantage:ApiKey"] ?? "";
        _baseUrl = _configuration["ApiSettings:AlphaVantage:BaseUrl"] ?? "";
        _rateLimitPerMinute = _configuration.GetValue<int>("ApiSettings:AlphaVantage:RateLimitPerMinute", 5);
        _rateLimitSemaphore = new SemaphoreSlim(_rateLimitPerMinute, _rateLimitPerMinute);
    }

    public async Task<Stock?> GetStockAsync(string symbol)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Alpha Vantage API key not configured");
            return null;
        }

        var cacheKey = $"stock_{symbol}";
        if (_cache.TryGetValue(cacheKey, out Stock? cachedStock))
        {
            return cachedStock;
        }

        await EnforceRateLimit();

        try
        {
            var url = $"{_baseUrl}?function=GLOBAL_QUOTE&symbol={symbol}&apikey={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var stock = ParseGlobalQuote(response, symbol);
            
            if (stock != null)
            {
                var cacheExpiry = TimeSpan.FromMinutes(_configuration.GetValue<int>("CacheSettings:DefaultExpirationMinutes", 15));
                _cache.Set(cacheKey, stock, cacheExpiry);
            }

            return stock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching stock data for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<List<Stock>> GetStocksAsync(List<string> symbols)
    {
        var tasks = symbols.Select(GetStockAsync);
        var results = await Task.WhenAll(tasks);
        return results.Where(s => s != null).ToList()!;
    }

    public async Task<List<Stock>> GetPopularStocksAsync()
    {
        var popularSymbols = new List<string> 
        { 
            "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", 
            "META", "NVDA", "NFLX", "AMD", "INTC",
            "SPY", "QQQ", "IWM", "DIA", "VTI"
        };
        
        return await GetStocksAsync(popularSymbols);
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        if (string.IsNullOrEmpty(_apiKey))
            return false;

        try
        {
            await EnforceRateLimit();
            var url = $"{_baseUrl}?function=GLOBAL_QUOTE&symbol=AAPL&apikey={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            return !string.IsNullOrEmpty(response) && !response.Contains("Error Message");
        }
        catch
        {
            return false;
        }
    }

    private async Task EnforceRateLimit()
    {
        await _rateLimitSemaphore.WaitAsync();
        
        var now = DateTime.UtcNow;
        var oneMinuteAgo = now.AddMinutes(-1);
        
        lock (_requestTimes)
        {
            _requestTimes.RemoveAll(t => t < oneMinuteAgo);
            _requestTimes.Add(now);
        }

        _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => _rateLimitSemaphore.Release());
    }

    private Stock? ParseGlobalQuote(string jsonResponse, string symbol)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonResponse);
            var root = document.RootElement;
            
            if (!root.TryGetProperty("Global Quote", out var quote))
                return null;

            var stock = new Stock
            {
                Symbol = symbol,
                CompanyName = symbol, 
                LastUpdated = DateTime.UtcNow
            };

            if (quote.TryGetProperty("05. price", out var priceElement) && 
                decimal.TryParse(priceElement.GetString(), out var price))
            {
                stock.Price = price;
            }

            if (quote.TryGetProperty("09. change", out var changeElement) && 
                decimal.TryParse(changeElement.GetString(), out var change))
            {
                stock.DayChange = change;
            }

            if (quote.TryGetProperty("10. change percent", out var changePercentElement))
            {
                var changePercentStr = changePercentElement.GetString()?.Replace("%", "");
                if (decimal.TryParse(changePercentStr, out var changePercent))
                {
                    stock.DayChangePercent = changePercent;
                }
            }

            if (quote.TryGetProperty("06. volume", out var volumeElement) && 
                long.TryParse(volumeElement.GetString(), out var volume))
            {
                stock.Volume = volume;
            }

            return stock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Alpha Vantage response for {Symbol}", symbol);
            return null;
        }
    }
}