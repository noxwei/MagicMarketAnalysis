using Microsoft.Data.Sqlite;
using Dapper;
using MagicMarketAnalysis.Models;

namespace MagicMarketAnalysis.Data;

public class DbInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(IConfiguration configuration, ILogger<DbInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=market_data.db";
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "SqliteSchema.sql");
            if (!File.Exists(schemaPath))
            {
                _logger.LogWarning("Schema file not found at {SchemaPath}", schemaPath);
                return;
            }

            var schemaSql = await File.ReadAllTextAsync(schemaPath);
            await connection.ExecuteAsync(schemaSql);

            _logger.LogInformation("Database schema initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database schema");
            throw;
        }
    }
}

public interface IStockRepository
{
    Task<Stock?> GetBySymbolAsync(string symbol);
    Task<List<Stock>> GetAllAsync();
    Task<ScreenerResult> SearchAsync(ScreenerRequest request);
    Task UpsertAsync(Stock stock);
    Task UpsertBatchAsync(List<Stock> stocks);
}

public class StockRepository : IStockRepository
{
    private readonly string _connectionString;
    private readonly ILogger<StockRepository> _logger;

    public StockRepository(IConfiguration configuration, ILogger<StockRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=market_data.db";
        _logger = logger;
    }

    public async Task<Stock?> GetBySymbolAsync(string symbol)
    {
        using var connection = new SqliteConnection(_connectionString);
        const string sql = "SELECT * FROM Stocks WHERE Symbol = @Symbol";
        
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Symbol = symbol });
        return row != null ? MapToStock(row) : null;
    }

    public async Task<List<Stock>> GetAllAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        const string sql = "SELECT * FROM Stocks ORDER BY Symbol";
        
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToStock).ToList();
    }

    public async Task<ScreenerResult> SearchAsync(ScreenerRequest request)
    {
        using var connection = new SqliteConnection(_connectionString);
        
        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (request.MinPE.HasValue)
        {
            whereClauses.Add("PERatio >= @MinPE");
            parameters.Add("MinPE", request.MinPE.Value);
        }

        if (request.MaxPE.HasValue)
        {
            whereClauses.Add("PERatio <= @MaxPE");
            parameters.Add("MaxPE", request.MaxPE.Value);
        }

        if (request.MinPrice.HasValue)
        {
            whereClauses.Add("Price >= @MinPrice");
            parameters.Add("MinPrice", request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            whereClauses.Add("Price <= @MaxPrice");
            parameters.Add("MaxPrice", request.MaxPrice.Value);
        }

        if (request.MinVolume.HasValue)
        {
            whereClauses.Add("Volume >= @MinVolume");
            parameters.Add("MinVolume", request.MinVolume.Value);
        }

        if (request.MinMarketCap.HasValue)
        {
            whereClauses.Add("MarketCap >= @MinMarketCap");
            parameters.Add("MinMarketCap", request.MinMarketCap.Value);
        }

        if (request.MaxMarketCap.HasValue)
        {
            whereClauses.Add("MarketCap <= @MaxMarketCap");
            parameters.Add("MaxMarketCap", request.MaxMarketCap.Value);
        }

        if (!string.IsNullOrEmpty(request.Sector))
        {
            whereClauses.Add("Sector = @Sector");
            parameters.Add("Sector", request.Sector);
        }

        var whereClause = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
        
        // Get total count
        var countSql = $"SELECT COUNT(*) FROM Stocks {whereClause}";
        var totalCount = await connection.QuerySingleAsync<int>(countSql, parameters);
        
        // Get paginated results
        var orderByClause = $"ORDER BY {request.SortBy} {(request.SortDescending ? "DESC" : "ASC")}";
        var limitClause = $"LIMIT @PageSize OFFSET @Offset";
        
        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", (request.PageNumber - 1) * request.PageSize);

        var sql = $"SELECT * FROM Stocks {whereClause} {orderByClause} {limitClause}";
        
        var rows = await connection.QueryAsync(sql, parameters);
        var stocks = rows.Select(MapToStock).ToList();

        return new ScreenerResult
        {
            Stocks = stocks,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task UpsertAsync(Stock stock)
    {
        using var connection = new SqliteConnection(_connectionString);
        const string sql = @"
            INSERT INTO Stocks (Symbol, Name, Price, MarketCap, Volume, PERatio, Sector, Industry, LastUpdated)
            VALUES (@Symbol, @CompanyName, @Price, @MarketCap, @Volume, @PERatio, @Sector, @Industry, @LastUpdated)
            ON CONFLICT(Symbol) DO UPDATE SET
                Name = @CompanyName,
                Price = @Price,
                MarketCap = @MarketCap,
                Volume = @Volume,
                PERatio = @PERatio,
                Sector = @Sector,
                Industry = @Industry,
                LastUpdated = @LastUpdated";

        await connection.ExecuteAsync(sql, stock);
    }

    public async Task UpsertBatchAsync(List<Stock> stocks)
    {
        if (!stocks.Any()) return;

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string sql = @"
                INSERT INTO Stocks (Symbol, Name, Price, MarketCap, Volume, PERatio, Sector, Industry, LastUpdated)
                VALUES (@Symbol, @CompanyName, @Price, @MarketCap, @Volume, @PERatio, @Sector, @Industry, @LastUpdated)
                ON CONFLICT(Symbol) DO UPDATE SET
                    Name = @CompanyName,
                    Price = @Price,
                    MarketCap = @MarketCap,
                    Volume = @Volume,
                    PERatio = @PERatio,
                    Sector = @Sector,
                    Industry = @Industry,
                    LastUpdated = @LastUpdated";

            await connection.ExecuteAsync(sql, stocks, transaction);
            await transaction.CommitAsync();

            _logger.LogInformation("Batch upserted {Count} stocks", stocks.Count);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to batch upsert stocks");
            throw;
        }
    }

    private static Stock MapToStock(dynamic row)
    {
        return new Stock
        {
            Symbol = row.Symbol,
            CompanyName = row.Name ?? row.Symbol,
            Price = row.Price ?? 0m,
            MarketCap = row.MarketCap,
            Volume = row.Volume ?? 0L,
            PERatio = row.PERatio,
            Sector = row.Sector,
            Industry = row.Industry,
            LastUpdated = row.LastUpdated ?? DateTime.UtcNow
        };
    }
}

public interface ISnapshotRepository
{
    Task<int> CreateSnapshotAsync(MarketSnapshot snapshot);
    Task<MarketSnapshot?> GetLatestAsync();
    Task<List<MarketSnapshot>> GetRecentAsync(int count = 10);
}

public class SnapshotRepository : ISnapshotRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SnapshotRepository> _logger;

    public SnapshotRepository(IConfiguration configuration, ILogger<SnapshotRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=market_data.db";
        _logger = logger;
    }

    public async Task<int> CreateSnapshotAsync(MarketSnapshot snapshot)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string snapshotSql = @"
                INSERT INTO Snapshots (Timestamp, Spy, Qqq, Dia, VixLevel, MarketStatus, TotalStocks)
                VALUES (@Timestamp, @SpyPrice, @QqqPrice, @DiaPrice, @VixLevel, @MarketStatus, @TotalStocks)
                RETURNING Id";

            var snapshotId = await connection.QuerySingleAsync<int>(snapshotSql, snapshot, transaction);

            if (snapshot.SectorPerformance?.Any() == true)
            {
                const string sectorSql = @"
                    INSERT INTO SectorPerformance (SnapshotId, Sector, ChangePercent)
                    VALUES (@SnapshotId, @Sector, @ChangePercent)";

                var sectorData = snapshot.SectorPerformance.Select(sp => new 
                { 
                    SnapshotId = snapshotId, 
                    sp.Sector, 
                    sp.ChangePercent 
                });

                await connection.ExecuteAsync(sectorSql, sectorData, transaction);
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Created market snapshot {SnapshotId} at {Timestamp}", snapshotId, snapshot.Timestamp);
            
            return snapshotId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create market snapshot");
            throw;
        }
    }

    public async Task<MarketSnapshot?> GetLatestAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        const string sql = "SELECT * FROM Snapshots ORDER BY Timestamp DESC LIMIT 1";
        
        var row = await connection.QueryFirstOrDefaultAsync(sql);
        return row != null ? MapToSnapshot(row) : null;
    }

    public async Task<List<MarketSnapshot>> GetRecentAsync(int count = 10)
    {
        using var connection = new SqliteConnection(_connectionString);
        const string sql = "SELECT * FROM Snapshots ORDER BY Timestamp DESC LIMIT @Count";
        
        var rows = await connection.QueryAsync(sql, new { Count = count });
        return rows.Select(MapToSnapshot).ToList();
    }

    private static MarketSnapshot MapToSnapshot(dynamic row)
    {
        return new MarketSnapshot
        {
            Timestamp = row.Timestamp,
            SpyPrice = row.Spy,
            QqqPrice = row.Qqq,
            DiaPrice = row.Dia,
            VixLevel = row.VixLevel,
            MarketStatus = row.MarketStatus,
            TotalStocks = row.TotalStocks,
            SectorPerformance = new List<SectorPerformance>()
        };
    }
}