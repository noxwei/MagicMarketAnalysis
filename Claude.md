# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Magic Market Analysis - Stock Screener

## Development Commands

### Build and Run
```bash
cd MagicMarketAnalysis
dotnet restore
dotnet build
dotnet run
```

### Development with Hot Reload
```bash
dotnet watch run
```

### Database Operations
```bash
# Reset SQLite database
rm -f market_data.db

# Run EF migrations (when implemented)
dotnet ef database update
```

### Testing
```bash
dotnet test
```

## Project Architecture

### Core Data Flow
1. **Timer Functions** (`Functions/SnapshotFunction.cs`) - Trigger every 15 minutes
2. **AggregatorService** - Orchestrates data collection from multiple APIs
3. **API Clients** - Individual service clients (FMP, Tradier, NewsData)
4. **SQLite Cache** - Local storage for market snapshots and historical data
5. **Screener Logic** - LINQ-based filtering of cached data
6. **Razor Pages** - Mobile-first UI for results display

### Service Layer Pattern
- **Interface Contracts**: `IMarketDataService`, `IFmpClient`, etc.
- **Refit HTTP Clients**: Auto-generated from interfaces with attributes
- **Polly Resilience**: 3 retry attempts with exponential backoff
- **Rate Limiting**: Custom semaphore-based throttling per API
- **Caching Strategy**: 15-minute expiry for real-time data, longer for historical

### Data Models Hierarchy
- **Stock** - Core equity data model
- **MarketSnapshot** - Point-in-time market state
- **ScreenerRequest/Result** - Filter criteria and paginated results
- **SectorPerformance** - Aggregated sector metrics

### API Integration Strategy
- **Primary**: Financial Modeling Prep for real-time quotes and fundamentals
- **Options**: Tradier for options flow data
- **News**: NewsData.io for headlines and sentiment
- **Backfill**: Alpha Vantage for historical data (one-time)

## Configuration Requirements

### Environment Variables
```bash
# Required for production
FMP_API_KEY=your_fmp_api_key
TRADIER_API_KEY=your_tradier_key
NEWSDATA_API_KEY=your_newsdata_key

# Optional for historical backfill
ALPHAVANTAGE_API_KEY=your_av_key
```

### appsettings.json Structure
- `ApiSettings:*:BaseUrl` - API endpoints
- `ApiSettings:*:RateLimitPer*` - Rate limiting thresholds
- `CacheSettings:DefaultExpirationMinutes` - Cache TTL

## Technical Constraints

### Security Guardrails
- **Server-side only**: No client-side API key exposure
- **Environment variables**: All sensitive config externalized
- **Rate limiting**: Respect API provider limits
- **Input validation**: All user inputs sanitized

### Performance Requirements
- **Response times**: Sub-30 seconds for screener results
- **Cache strategy**: 15-minute market data, longer for static data
- **Database**: SQLite for local dev, Azure SQL for production
- **Mobile-first**: Responsive design with desktop compatibility

### Data Source Priorities
1. **FMP** - Primary market data ($22/mo)
2. **Tradier** - Options flow ($10/mo)
3. **NewsData** - Headlines (200 free/day)
4. **Alpha Vantage** - Backfill only (free)

## Development Patterns

### API Client Implementation
- Use **Refit** for HTTP client generation
- Implement **Polly** retry policies (3 attempts, exponential backoff)
- Inject **IHttpClientFactory** for connection pooling
- Add **custom rate limiting** per provider requirements

### Error Handling Strategy
- **Graceful degradation**: Continue with partial data if one API fails
- **Logging**: Structured logging with Serilog
- **Fallbacks**: Secondary data sources when primary fails
- **User feedback**: Clear error messages without exposing internals

### Screener Filter Logic
- **LINQ-based**: Simple filtering on cached data
- **5-7 core filters**: P/E, RSI, Volume, Price, MarketCap, Sector
- **Pagination**: 50 results per page default
- **Sorting**: Multiple column support

## Phase-Based Development

### Current Phase: MVP Backend
- SQLite schema and models
- FMP client with basic endpoints
- Timer-triggered data collection
- Basic screener filters
- Razor Pages UI foundation

### Next: UI Polish
- Mobile-responsive forms
- Results table with sorting
- Saved preset management
- CSV export functionality

### Future: Enhanced Features
- Options flow integration
- News headlines display
- Sector heatmap visualization
- Performance optimizations

## Code Generation Guidelines

### DO Generate
- Clean .NET 8 code with nullable reference types
- Refit interfaces with proper HTTP attributes
- Polly retry policies for resilience
- Entity models with proper relationships
- Responsive CSS without heavy frameworks

### DO NOT Generate
- Authentication/authorization (single-user initially)
- Heavy ORM usage (prefer Dapper for SQLite)
- Real-time features (SignalR/WebSockets)
- JavaScript frameworks (vanilla JS only)
- AI/ML inference libraries

## API Budget Awareness

### Monthly Costs: ~$32
- FMP Starter: $22/month (5 years history)
- Tradier Dev: $10/month (options access)
- NewsData: Free tier (200/day)
- Alpha Vantage: Free (one-time backfill)

### Usage Optimization
- Cache aggressively to minimize API calls
- Batch requests where possible
- Implement circuit breakers for cost protection
- Monitor usage via logging and metrics

---

*This file ensures consistent development patterns and maintains the project's technical vision.*