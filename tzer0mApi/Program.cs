using tzer0mApi.Services.Middleware;
using tzer0mApi.Services.SmarterMeter;
using tzer0mApi.Services.Ting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<TingService>();
builder.Services.AddHttpClient<VisionService>();
builder.Services.AddSingleton<DatabaseService>();

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