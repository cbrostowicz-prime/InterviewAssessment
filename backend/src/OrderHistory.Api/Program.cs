using Microsoft.EntityFrameworkCore;
using OrderHistory.Api.Data;
using OrderHistory.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

var connectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("ConnectionStrings:OrderDb is not configured.");

builder.Services.AddDbContext<OrderDbContext>(opts =>
    opts.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null)));

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<UserStatsService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

using (var ctsScope = new CancellationTokenSource(TimeSpan.FromMinutes(2)))
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    await DbInitializer.InitializeAsync(app.Services, startupLogger, ctsScope.Token);
}

app.Run();

public partial class Program;
