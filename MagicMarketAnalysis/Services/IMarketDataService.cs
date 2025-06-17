using MagicMarketAnalysis.Models;

namespace MagicMarketAnalysis.Services;

public interface IMarketDataService
{
    Task<Stock?> GetStockAsync(string symbol);
    Task<List<Stock>> GetStocksAsync(List<string> symbols);
    Task<List<Stock>> GetPopularStocksAsync();
    Task<bool> IsServiceAvailableAsync();
}