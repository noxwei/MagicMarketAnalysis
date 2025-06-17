using MagicMarketAnalysis.Models;
using MagicMarketAnalysis.Data;

namespace MagicMarketAnalysis.Services;

public interface IAggregatorService
{
    Task<MarketSnapshot> CollectMarketDataAsync();
    Task<int> SaveSnapshotAsync(MarketSnapshot snapshot);
}

public class AggregatorService : IAggregatorService
{
    private readonly IFmpClient _fmpClient;
    private readonly IStockRepository _stockRepository;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly ILogger<AggregatorService> _logger;

    private readonly string[] _majorIndices = { "SPY", "QQQ", "DIA", "VIX" };
    private readonly string[] _popularStocks = {
        "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "META", "NVDA", "NFLX", "AMD", "INTC",
        "JPM", "JNJ", "V", "PG", "UNH", "HD", "MA", "BAC", "ABBV", "PFE"
    };

    public AggregatorService(
        IFmpClient fmpClient,
        IStockRepository stockRepository,
        ISnapshotRepository snapshotRepository,
        ILogger<AggregatorService> logger)
    {
        _fmpClient = fmpClient;
        _stockRepository = stockRepository;
        _snapshotRepository = snapshotRepository;
        _logger = logger;
    }

    public async Task<MarketSnapshot> CollectMarketDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting market data collection");

            var snapshot = new MarketSnapshot
            {
                Timestamp = DateTime.UtcNow
            };

            // Collect major indices data
            await CollectIndicesAsync(snapshot);

            // Collect sector performance
            await CollectSectorPerformanceAsync(snapshot);

            // Collect and cache popular stocks
            await CollectPopularStocksAsync(snapshot);

            _logger.LogInformation("Market data collection completed. SPY: {Spy}, QQQ: {Qqq}, Total Stocks: {TotalStocks}",
                snapshot.SpyPrice, snapshot.QqqPrice, snapshot.TotalStocks);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect market data");
            throw;
        }
    }

    public async Task<int> SaveSnapshotAsync(MarketSnapshot snapshot)
    {
        try
        {
            var snapshotId = await _snapshotRepository.CreateSnapshotAsync(snapshot);
            _logger.LogInformation("Saved market snapshot {SnapshotId}", snapshotId);
            return snapshotId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save market snapshot");
            throw;
        }
    }

    private async Task CollectIndicesAsync(MarketSnapshot snapshot)
    {
        try
        {
            var indicesQuotes = await _fmpClient.GetMultipleQuotesAsync(_majorIndices);

            foreach (var quote in indicesQuotes)
            {
                switch (quote.Symbol.ToUpper())
                {
                    case "SPY":
                        snapshot.SpyPrice = quote.Price;
                        break;
                    case "QQQ":
                        snapshot.QqqPrice = quote.Price;
                        break;
                    case "DIA":
                        snapshot.DiaPrice = quote.Price;
                        break;
                    case "VIX":
                        snapshot.VixLevel = quote.Price;
                        break;
                }
            }

            // Determine market status based on market hours
            snapshot.MarketStatus = DetermineMarketStatus();

            _logger.LogDebug("Collected indices data: SPY={Spy}, QQQ={Qqq}, DIA={Dia}, VIX={Vix}",
                snapshot.SpyPrice, snapshot.QqqPrice, snapshot.DiaPrice, snapshot.VixLevel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect indices data");
        }
    }

    private async Task CollectSectorPerformanceAsync(MarketSnapshot snapshot)
    {
        try
        {
            var sectorData = await _fmpClient.GetSectorPerfAsync();
            
            snapshot.SectorPerformance = sectorData.Select(sp => new SectorPerformance
            {
                Sector = sp.Sector,
                ChangePercent = ParseChangePercent(sp.ChangesPercentage)
            }).ToList();

            _logger.LogDebug("Collected {Count} sector performance records", snapshot.SectorPerformance.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect sector performance data");
            snapshot.SectorPerformance = new List<SectorPerformance>();
        }
    }

    private async Task CollectPopularStocksAsync(MarketSnapshot snapshot)
    {
        try
        {
            var stockQuotes = await _fmpClient.GetMultipleQuotesAsync(_popularStocks);
            var stocks = new List<Stock>();

            foreach (var quote in stockQuotes)
            {
                var stock = new Stock
                {
                    Symbol = quote.Symbol,
                    CompanyName = quote.Name,
                    Price = quote.Price,
                    Volume = quote.Volume,
                    MarketCap = quote.MarketCap,
                    PERatio = quote.Pe,
                    DayChange = quote.Change,
                    DayChangePercent = quote.ChangesPercentage,
                    LastUpdated = DateTime.UtcNow
                };

                // Fetch additional profile data for sector/industry if needed
                try
                {
                    var profile = await _fmpClient.GetCompanyProfileAsync(quote.Symbol);
                    if (profile != null)
                    {
                        stock.Sector = profile.Sector;
                        stock.Industry = profile.Industry;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not fetch profile for {Symbol}", quote.Symbol);
                }

                stocks.Add(stock);
            }

            // Batch save to database
            if (stocks.Any())
            {
                await _stockRepository.UpsertBatchAsync(stocks);
                snapshot.TotalStocks = stocks.Count;
            }

            _logger.LogDebug("Collected and cached {Count} popular stocks", stocks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect popular stocks data");
            snapshot.TotalStocks = 0;
        }
    }

    private string DetermineMarketStatus()
    {
        var now = DateTime.Now;
        var easternTime = TimeZoneInfo.ConvertTimeToUtc(now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
        var marketOpen = new TimeSpan(9, 30, 0); // 9:30 AM EST
        var marketClose = new TimeSpan(16, 0, 0); // 4:00 PM EST

        if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
        {
            return "Closed (Weekend)";
        }

        var currentTime = now.TimeOfDay;
        if (currentTime >= marketOpen && currentTime <= marketClose)
        {
            return "Open";
        }
        else if (currentTime < marketOpen)
        {
            return "Pre-Market";
        }
        else
        {
            return "After-Hours";
        }
    }

    private decimal ParseChangePercent(string changePercentage)
    {
        if (string.IsNullOrEmpty(changePercentage))
            return 0m;

        var cleaned = changePercentage.Replace("%", "").Replace("+", "").Trim();
        return decimal.TryParse(cleaned, out var result) ? result : 0m;
    }
}