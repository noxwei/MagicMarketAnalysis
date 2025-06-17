using Microsoft.Extensions.Hosting;
using MagicMarketAnalysis.Services;

namespace MagicMarketAnalysis.Functions;

public class SnapshotFunction : BackgroundService
{
    private readonly IAggregatorService _aggregatorService;
    private readonly ILogger<SnapshotFunction> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public SnapshotFunction(IAggregatorService aggregatorService, ILogger<SnapshotFunction> logger)
    {
        _aggregatorService = aggregatorService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Market snapshot function started. Interval: {Interval} minutes", _interval.TotalMinutes);

        // Run immediately on startup for testing
        await RunSnapshotCollection();

        // Then run on schedule
        using var timer = new PeriodicTimer(_interval);
        
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSnapshotCollection();
        }

        _logger.LogInformation("Market snapshot function stopped");
    }

    private async Task RunSnapshotCollection()
    {
        try
        {
            _logger.LogInformation("Starting scheduled market data collection");
            
            var snapshot = await _aggregatorService.CollectMarketDataAsync();
            var snapshotId = await _aggregatorService.SaveSnapshotAsync(snapshot);
            
            _logger.LogInformation("Market snapshot {SnapshotId} completed successfully at {Timestamp}. " +
                "SPY: ${Spy:F2}, QQQ: ${Qqq:F2}, Stocks: {TotalStocks}",
                snapshotId, snapshot.Timestamp, snapshot.SpyPrice, snapshot.QqqPrice, snapshot.TotalStocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Market snapshot collection failed");
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Market snapshot function is starting");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Market snapshot function is stopping");
        await base.StopAsync(cancellationToken);
    }
}

// Alternative implementation using Timer for more precise control
public class TimerBasedSnapshotFunction : IHostedService, IDisposable
{
    private readonly IAggregatorService _aggregatorService;
    private readonly ILogger<TimerBasedSnapshotFunction> _logger;
    private Timer? _timer;

    public TimerBasedSnapshotFunction(IAggregatorService aggregatorService, ILogger<TimerBasedSnapshotFunction> logger)
    {
        _aggregatorService = aggregatorService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Timer-based snapshot function starting");
        
        // Calculate initial delay to sync with 15-minute intervals (e.g., :00, :15, :30, :45)
        var now = DateTime.Now;
        var nextRun = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 15 + 1) * 15, 0);
        if (nextRun.Minute >= 60)
        {
            nextRun = nextRun.AddHours(1).AddMinutes(-60);
        }
        
        var initialDelay = nextRun - now;
        if (initialDelay < TimeSpan.Zero)
        {
            initialDelay = TimeSpan.Zero;
        }

        _logger.LogInformation("Next snapshot scheduled for {NextRun} (in {Delay})", nextRun, initialDelay);

        _timer = new Timer(DoWork, null, initialDelay, TimeSpan.FromMinutes(15));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("Timer-triggered market data collection starting");
            
            var snapshot = await _aggregatorService.CollectMarketDataAsync();
            var snapshotId = await _aggregatorService.SaveSnapshotAsync(snapshot);
            
            _logger.LogInformation("Timer-triggered snapshot {SnapshotId} completed. " +
                "Market Status: {MarketStatus}, SPY: ${Spy:F2}, Total Stocks: {TotalStocks}",
                snapshotId, snapshot.MarketStatus, snapshot.SpyPrice, snapshot.TotalStocks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timer-triggered snapshot collection failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Timer-based snapshot function stopping");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}