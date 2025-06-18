using Microsoft.AspNetCore.Mvc;
using MagicMarketAnalysis.Data;
using MagicMarketAnalysis.Services;
using MagicMarketAnalysis.Models;

namespace MagicMarketAnalysis.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
    private readonly IStockRepository _stockRepository;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IAggregatorService _aggregatorService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(
        IStockRepository stockRepository,
        ISnapshotRepository snapshotRepository,
        IAggregatorService aggregatorService,
        ILogger<ApiController> logger)
    {
        _stockRepository = stockRepository;
        _snapshotRepository = snapshotRepository;
        _aggregatorService = aggregatorService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var latestSnapshot = await _snapshotRepository.GetLatestAsync();
            var cachedStocks = await _stockRepository.GetAllAsync();
            var topStocks = cachedStocks.OrderByDescending(s => s.Volume).Take(10).ToList();

            return Ok(new
            {
                LatestSnapshot = latestSnapshot,
                TopStocks = topStocks,
                TotalStocksCached = cachedStocks.Count,
                LastUpdate = latestSnapshot?.Timestamp,
                Status = "Healthy"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dashboard data");
            return StatusCode(500, new { Error = "Failed to load dashboard data" });
        }
    }

    [HttpGet("stocks")]
    public async Task<IActionResult> GetStocks(
        decimal? minPE = null,
        decimal? maxPE = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        long? minVolume = null,
        decimal? minMarketCap = null,
        string? sector = null,
        string sortBy = "Symbol",
        bool sortDescending = false,
        int page = 1,
        int pageSize = 20)
    {
        try
        {
            var request = new Models.ScreenerRequest
            {
                MinPE = minPE,
                MaxPE = maxPE,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinVolume = minVolume,
                MinMarketCap = minMarketCap,
                Sector = sector,
                SortBy = sortBy,
                SortDescending = sortDescending,
                PageNumber = page,
                PageSize = pageSize
            };

            var result = await _stockRepository.SearchAsync(request);

            return Ok(new
            {
                stocks = result.Stocks,
                totalCount = result.TotalCount,
                page = result.PageNumber,
                pageSize = result.PageSize,
                totalPages = result.TotalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search stocks");
            return StatusCode(500, new { Error = "Failed to search stocks" });
        }
    }

    [HttpPost("collect-data")]
    public async Task<IActionResult> CollectData()
    {
        try
        {
            _logger.LogInformation("Manual data collection triggered");
            
            var snapshot = await _aggregatorService.CollectMarketDataAsync();
            var snapshotId = await _aggregatorService.SaveSnapshotAsync(snapshot);
            
            return Ok(new
            {
                Message = "Data collection completed",
                SnapshotId = snapshotId,
                Timestamp = snapshot.Timestamp,
                SPY = snapshot.SpyPrice,
                QQQ = snapshot.QqqPrice,
                DIA = snapshot.DiaPrice,
                StocksCached = snapshot.TotalStocks,
                SectorCount = snapshot.SectorPerformance.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual data collection failed");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshots(int count = 10)
    {
        try
        {
            var snapshots = await _snapshotRepository.GetRecentAsync(count);
            return Ok(snapshots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get snapshots");
            return StatusCode(500, new { Error = "Failed to get snapshots" });
        }
    }

    [HttpPost("seed-sample-data")]
    public async Task<IActionResult> SeedSampleData()
    {
        try
        {
            // Create sample stock data for testing
            var sampleStocks = new List<Stock>
            {
                new() { Symbol = "AAPL", CompanyName = "Apple Inc.", Price = 175.23m, Volume = 45123456, MarketCap = 2800000000000m, PERatio = 28.5m, DayChange = 2.34m, DayChangePercent = 1.35m, Sector = "Technology", Industry = "Consumer Electronics", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "MSFT", CompanyName = "Microsoft Corporation", Price = 342.67m, Volume = 32456789, MarketCap = 2540000000000m, PERatio = 32.1m, DayChange = -1.23m, DayChangePercent = -0.36m, Sector = "Technology", Industry = "Software", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "GOOGL", CompanyName = "Alphabet Inc.", Price = 128.45m, Volume = 28934567, MarketCap = 1630000000000m, PERatio = 25.8m, DayChange = 3.21m, DayChangePercent = 2.56m, Sector = "Communication Services", Industry = "Internet Content & Information", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "AMZN", CompanyName = "Amazon.com Inc.", Price = 142.18m, Volume = 41234567, MarketCap = 1480000000000m, PERatio = 52.3m, DayChange = -2.45m, DayChangePercent = -1.69m, Sector = "Consumer Cyclical", Industry = "Internet Retail", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "TSLA", CompanyName = "Tesla Inc.", Price = 248.73m, Volume = 67890123, MarketCap = 790000000000m, PERatio = 65.2m, DayChange = 8.34m, DayChangePercent = 3.47m, Sector = "Consumer Cyclical", Industry = "Auto Manufacturers", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "META", CompanyName = "Meta Platforms Inc.", Price = 315.42m, Volume = 19876543, MarketCap = 820000000000m, PERatio = 23.7m, DayChange = 4.56m, DayChangePercent = 1.47m, Sector = "Communication Services", Industry = "Internet Content & Information", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "NVDA", CompanyName = "NVIDIA Corporation", Price = 875.34m, Volume = 55432198, MarketCap = 2160000000000m, PERatio = 68.4m, DayChange = 12.45m, DayChangePercent = 1.44m, Sector = "Technology", Industry = "Semiconductors", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "JPM", CompanyName = "JPMorgan Chase & Co.", Price = 154.67m, Volume = 12345678, MarketCap = 452000000000m, PERatio = 12.3m, DayChange = -0.89m, DayChangePercent = -0.57m, Sector = "Financial Services", Industry = "Banks", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "JNJ", CompanyName = "Johnson & Johnson", Price = 162.34m, Volume = 8765432, MarketCap = 428000000000m, PERatio = 15.6m, DayChange = 1.12m, DayChangePercent = 0.69m, Sector = "Healthcare", Industry = "Drug Manufacturers", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "V", CompanyName = "Visa Inc.", Price = 245.89m, Volume = 7654321, MarketCap = 520000000000m, PERatio = 31.2m, DayChange = 2.67m, DayChangePercent = 1.10m, Sector = "Financial Services", Industry = "Credit Services", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "PG", CompanyName = "Procter & Gamble Co.", Price = 156.78m, Volume = 6543210, MarketCap = 375000000000m, PERatio = 26.1m, DayChange = 0.45m, DayChangePercent = 0.29m, Sector = "Consumer Defensive", Industry = "Household & Personal Products", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "UNH", CompanyName = "UnitedHealth Group Inc.", Price = 512.45m, Volume = 3456789, MarketCap = 485000000000m, PERatio = 24.8m, DayChange = -3.21m, DayChangePercent = -0.62m, Sector = "Healthcare", Industry = "Healthcare Plans", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "HD", CompanyName = "Home Depot Inc.", Price = 325.67m, Volume = 4567890, MarketCap = 335000000000m, PERatio = 22.5m, DayChange = 1.89m, DayChangePercent = 0.58m, Sector = "Consumer Cyclical", Industry = "Home Improvement Retail", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "MA", CompanyName = "Mastercard Inc.", Price = 398.12m, Volume = 2345678, MarketCap = 385000000000m, PERatio = 33.7m, DayChange = 4.23m, DayChangePercent = 1.07m, Sector = "Financial Services", Industry = "Credit Services", LastUpdated = DateTime.UtcNow },
                new() { Symbol = "BAC", CompanyName = "Bank of America Corp.", Price = 34.56m, Volume = 45678901, MarketCap = 285000000000m, PERatio = 11.2m, DayChange = -0.34m, DayChangePercent = -0.97m, Sector = "Financial Services", Industry = "Banks", LastUpdated = DateTime.UtcNow }
            };

            await _stockRepository.UpsertBatchAsync(sampleStocks);

            return Ok(new
            {
                message = "Sample data seeded successfully",
                stocksAdded = sampleStocks.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed sample data");
            return StatusCode(500, new { error = "Failed to seed sample data", details = ex.Message });
        }
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return Ok(new
        {
            Service = "Magic Market Analysis API",
            Version = "1.0.0",
            Endpoints = new[]
            {
                "GET /api/dashboard - Market overview with latest data",
                "GET /api/stocks - Stock screener with filters",
                "POST /api/collect-data - Trigger data collection manually",
                "POST /api/seed-sample-data - Add sample stock data for testing",
                "GET /api/snapshots - Recent market snapshots",
                "GET /health - Health check"
            },
            Documentation = "See CLAUDE.md for full API details"
        });
    }
}