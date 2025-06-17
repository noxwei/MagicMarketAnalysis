# Magic Market Analysis

A professional stock screener built to replace Finviz with API-driven data ownership, targeting personal use and quantitative analysts. Built with .NET 8 Razor Pages for mobile-first usage.

## 🎯 Vision

**Own the data pipeline** - Move away from Finviz dependency with direct API integration and local caching.

**Target Users**: 
- Personal use dashboard
- Small group of quantitative analysts
- Future premium features behind paywall

## ⚡ Key Features

### Current (MVP)
- **Stock Screener**: P/E ratio, RSI, volume, price range, market cap filtering
- **Real-time Data**: Financial Modeling Prep integration with 15-min updates
- **Mobile-First**: Responsive design optimized for mobile with desktop compatibility
- **Local Cache**: SQLite storage for historical data and performance

### Planned (Phase 2+)
- **Options Flow**: Tradier integration for options data
- **News Feed**: NewsData.io headlines and sentiment
- **Heatmaps**: Sector performance visualization
- **Saved Presets**: Custom screener configurations ("Earnings Week", "Value Tech")

## 🛠 Tech Stack

- **Backend**: .NET 8 with Razor Pages
- **Database**: SQLite (local cache) → Azure SQL (production)
- **APIs**: Financial Modeling Prep, Tradier, NewsData.io
- **Frontend**: Vanilla JavaScript, CSS Grid/Flexbox
- **Deployment**: Azure App Service or Docker

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- API Keys (see Configuration section)

### Run Locally
```bash
git clone <repo-url>
cd MagicMarketAnalysis/MagicMarketAnalysis
dotnet restore
dotnet run
```

Navigate to `https://localhost:5001`

## ⚙️ Configuration

### Environment Variables
```bash
# Required API Keys
export FMP_API_KEY="your_fmp_key_here"
export TRADIER_API_KEY="your_tradier_key_here" 
export NEWSDATA_API_KEY="your_newsdata_key_here"

# Optional (for historical backfill)
export ALPHAVANTAGE_API_KEY="your_av_key_here"
```

### API Costs
- **FMP Starter**: $22/month (5 years historical data)
- **Tradier Dev**: $10/month (options flow access)
- **NewsData.io**: 200 headlines/day free tier
- **Alpha Vantage**: Free tier for one-time backfill

**Total Monthly Cost**: ~$32/month for full functionality

## 📊 Data Sources

1. **Financial Modeling Prep** - Primary stock data, fundamentals, historical
2. **Tradier** - Options flow, real-time market data
3. **NewsData.io** - Market news and headlines
4. **Alpha Vantage** - Historical backfill (one-time usage)

## 🏗 Architecture

### Key Principles
- **Server-side API calls only** - No client-side key exposure
- **Rate limiting** - Respect API limits with intelligent caching
- **Lightweight** - SQLite for local cache, minimal dependencies
- **Security-first** - Environment variables, no keys in code

### Project Structure
```
MagicMarketAnalysis/
├── Services/           # API clients and data aggregation
├── Models/            # Data models and DTOs
├── Pages/             # Razor Pages UI
├── Functions/         # Background jobs (Timer triggers)
└── wwwroot/          # Static assets
```

## 🔄 Data Pipeline

1. **Timer Function** runs every 15 minutes
2. **AggregatorService** fetches data from multiple APIs
3. **SQLite cache** stores market snapshots locally
4. **Screener logic** filters cached data based on user criteria
5. **Razor Pages** render results with responsive UI

## 📈 Roadmap

### Phase 1 (MVP) - Days 1-5
- [x] Project setup and configuration
- [x] Basic models and structure
- [ ] FMP API integration
- [ ] SQLite schema and caching
- [ ] Basic screener filters

### Phase 2 (UI) - Days 6-9
- [ ] Mobile-responsive interface
- [ ] Filter forms and validation
- [ ] Results table with sorting
- [ ] Saved presets functionality

### Phase 3 (Enhanced) - Days 10-14
- [ ] Tradier options integration
- [ ] News headlines feed
- [ ] Sector heatmap visualization
- [ ] CSV export functionality

### Phase 4 (Production) - Days 15+
- [ ] Docker containerization
- [ ] Azure deployment
- [ ] Monitoring and logging
- [ ] Performance optimizations

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run with hot reload for development
dotnet watch run
```

## 📝 Contributing

This is a personal project, but contributions are welcome:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

Private project - All rights reserved

## 🔗 Links

- [Financial Modeling Prep API](https://financialmodelingprep.com/developer/docs)
- [Tradier Developer Docs](https://developer.tradier.com)
- [NewsData.io API](https://newsdata.io/docs)

---

*Professional market analysis tool built for quantitative traders and analysts.*