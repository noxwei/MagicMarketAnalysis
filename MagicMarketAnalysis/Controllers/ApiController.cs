using Microsoft.AspNetCore.Mvc;
using MagicMarketAnalysis.Data;
using MagicMarketAnalysis.Services;

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

            var stocks = await _stockRepository.SearchAsync(request);
            var totalCount = await _stockRepository.GetAllAsync();

            return Ok(new
            {
                Stocks = stocks,
                TotalCount = totalCount.Count,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount.Count / pageSize),
                Request = request
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
                "GET /api/snapshots - Recent market snapshots",
                "GET /health - Health check"
            },
            Documentation = "See CLAUDE.md for full API details"
        });
    }
}