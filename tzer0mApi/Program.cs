using Microsoft.EntityFrameworkCore;
using tzer0mApi.Services.Keys;
using tzer0mApi.Services.Middleware;
using tzer0mApi.Services.SmarterMeter;
using tzer0mApi.Services.StockWise;
using tzer0mApi.Services.Ting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TingService>();
builder.Services.AddHttpClient<VisionService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddScoped<KeysService>();
builder.Services.AddScoped<CalculationService>();
builder.Services.AddDbContext<StockWiseDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("StockWise")));

// Build app
WebApplication app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/", () => Results.Redirect("/swagger", true)).ExcludeFromDescription();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseApiKeyMiddleware();
app.MapControllers();
app.Run();