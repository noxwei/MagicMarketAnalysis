-- Magic Market Analysis SQLite Schema
-- Local cache for market data and user presets

CREATE TABLE IF NOT EXISTS Stocks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Symbol TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Price DECIMAL(10,2),
    MarketCap BIGINT,
    Volume BIGINT,
    PERatio DECIMAL(8,2),
    Sector TEXT,
    Industry TEXT,
    LastUpdated DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UK_Stocks_Symbol UNIQUE (Symbol)
);

CREATE TABLE IF NOT EXISTS Snapshots (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Spy DECIMAL(8,2),
    Qqq DECIMAL(8,2),
    Dia DECIMAL(8,2),
    VixLevel DECIMAL(6,2),
    MarketStatus TEXT,
    TotalStocks INTEGER DEFAULT 0,
    CONSTRAINT UK_Snapshots_Timestamp UNIQUE (Timestamp)
);

CREATE TABLE IF NOT EXISTS Presets (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Json TEXT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UK_Presets_Name UNIQUE (Name)
);

CREATE TABLE IF NOT EXISTS SectorPerformance (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SnapshotId INTEGER NOT NULL,
    Sector TEXT NOT NULL,
    ChangePercent DECIMAL(6,2),
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SnapshotId) REFERENCES Snapshots(Id),
    CONSTRAINT UK_SectorPerf_Snapshot_Sector UNIQUE (SnapshotId, Sector)
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS IX_Stocks_Symbol ON Stocks(Symbol);
CREATE INDEX IF NOT EXISTS IX_Stocks_LastUpdated ON Stocks(LastUpdated);
CREATE INDEX IF NOT EXISTS IX_Snapshots_Timestamp ON Snapshots(Timestamp);
CREATE INDEX IF NOT EXISTS IX_SectorPerf_SnapshotId ON SectorPerformance(SnapshotId);
CREATE INDEX IF NOT EXISTS IX_SectorPerf_Sector ON SectorPerformance(Sector);

-- Sample presets for development
INSERT OR IGNORE INTO Presets (Name, Json) VALUES 
('Value Stocks', '{"MinPE": 5, "MaxPE": 15, "MinMarketCap": 1000000000, "SortBy": "PERatio"}'),
('Growth Tech', '{"Sector": "Technology", "MinMarketCap": 10000000000, "SortBy": "MarketCap", "SortDescending": true}'),
('High Volume', '{"MinVolume": 10000000, "SortBy": "Volume", "SortDescending": true}');