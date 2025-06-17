using MagicMarketAnalysis.Data;
using MagicMarketAnalysis.Services;
using MagicMarketAnalysis.Functions;
using Refit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/magic-market-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Add HTTP clients
builder.Services.AddHttpClient();

// Configure Refit clients
builder.Services.AddHttpClient("FmpClient", client =>
{
    client.BaseAddress = new Uri("https://financialmodelingprep.com/api/v3/");
    client.DefaultRequestHeaders.Add("User-Agent", "MagicMarketAnalysis/1.0");
});

// Register repositories
builder.Services.AddSingleton<DbInitializer>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<ISnapshotRepository, SnapshotRepository>();

// Register API clients
builder.Services.AddScoped<IFmpClient, FmpClient>();

// Register services
builder.Services.AddScoped<IAggregatorService, AggregatorService>();

// Register background services (commented out for now - will enable after API testing)
// builder.Services.AddHostedService<SnapshotFunction>();

// Add connection string
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await dbInitializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow });

Log.Information("Magic Market Analysis starting up");

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
