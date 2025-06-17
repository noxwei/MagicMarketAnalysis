namespace MagicMarketAnalysis.Models;

public class MarketSnapshot
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public decimal? SpyPrice { get; set; }
    public decimal? QqqPrice { get; set; }
    public decimal? DiaPrice { get; set; }
    public decimal? VixLevel { get; set; }
    public string? MarketStatus { get; set; }
    public int TotalStocks { get; set; }
    public List<SectorPerformance> SectorPerformance { get; set; } = new();
}

public class SectorPerformance
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public required string Sector { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}