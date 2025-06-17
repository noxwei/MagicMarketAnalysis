# Changelog

All notable changes to Magic Market Analysis will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure with .NET 8 Razor Pages
- SQLite integration for local data caching
- Financial Modeling Prep API client setup
- Basic stock screener models and DTOs
- Mobile-first responsive design framework
- Timer-triggered data aggregation service

### Changed
- Migrated from Alpha Vantage-first approach to FMP-primary strategy
- Updated data source priority: FMP → Tradier → NewsData → Alpha Vantage backup

### Security
- Environment variable configuration for all API keys
- Server-side API calls only (no client-side key exposure)

## [0.1.0] - 2024-01-XX

### Major Updates & Architecture Decisions

#### Project Evolution
- **Renamed**: `FinvizReplacement` → `Magic Market Analysis`
- **Vision Shift**: From simple Finviz clone to comprehensive market analysis platform
- **Data Strategy**: Moved from free-tier reliance to premium API ownership model

#### Technical Architecture Changes

**Database Strategy**:
- **Before**: "Stateless design - no database unless explicitly specified"
- **After**: "Lightweight SQLite for local historical cache; upgrade to Azure SQL for production"
- **Rationale**: Historical data storage essential for backtesting and performance analysis

**Data Source Restructure**:
- **Before**: 
  1. Alpha Vantage (5 calls/min, 25/day free)
  2. Yahoo Finance (unofficial backup)
  3. Polygon.io (5 calls/min free)
- **After**: 
  1. Financial Modeling Prep ($22/mo, 5yr history)
  2. Tradier Market Data ($10/mo, options flow)
  3. NewsData.io (200 headlines/day free)
  4. Alpha Vantage (one-time backfill, free)

**Service Layer Updates**:
- Added `FmpClient.cs` - Primary market data client
- Added `TradierClient.cs` - Options flow integration
- Added `NewsClient.cs` - Headlines and sentiment
- Added `AvBackfillClient.cs` - Historical data backfill
- Added `AggregatorService.cs` - Multi-source data consolidation

**Scheduled Processing**:
- Added `SnapshotFunction.cs` - Timer trigger every 15 minutes
- Added `MarketSnapshot.cs` model for timestamped market state
- Implemented background data collection pipeline

#### Budget & Cost Model
- **Monthly API Costs**: ~$32/month (FMP $22 + Tradier $10)
- **ROI Strategy**: Premium features behind paywall for Phase 2
- **Cost Consciousness**: Free tiers where possible, clear upgrade paths

#### Future-Proofing Decisions

**AI Integration Guard Rails**:
- **Core Principle**: "No AI/ML inference layers - pure data filtering only"
- **Exception**: Single Claude summary endpoint for daily market pulse (optional)
- **Rationale**: Keep core functionality fast and reliable, optional AI enhancements

**Scalability Considerations**:
- SQLite → Azure SQL migration path planned
- In-memory cache → Redis upgrade path documented
- Monolithic → microservices refactor strategy outlined

### Development Workflow Changes
- Implemented todo-driven development with Claude Code
- Added CLAUDE.md as single source of truth for LLM instructions
- Established changelog discipline for major architectural decisions

### Security Enhancements
- All API keys moved to environment variables
- Server-side rate limiting implementation
- Zero client-side credential exposure policy

---

## Notes

### Why Document Architecture Changes?
This changelog serves as both a historical record and future-proofing documentation. When working with LLMs or onboarding new developers, having clear context about why certain decisions were made prevents regression to inferior patterns.

### LLM Instruction Evolution
The `CLAUDE.md` file has been updated to reflect the new architecture. Key changes ensure future LLM interactions will:
- Suggest FMP over Alpha Vantage for primary data
- Include SQLite storage in any data pipeline suggestions  
- Respect the $32/month API budget in feature recommendations
- Follow the established service layer patterns

### Success Metrics
- **Technical**: Sub-30 second screener response times
- **Business**: Personal usage daily, 5+ saved presets
- **Quality**: Zero API key exposures, 99% uptime
- **Evolution**: Ready for Phase 2 premium features

---

*This changelog maintains transparency about major technical decisions and serves as architectural documentation for future development.*