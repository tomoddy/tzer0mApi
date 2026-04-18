using tzer0mApi.Services.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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