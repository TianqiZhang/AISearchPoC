using AISearchPoC.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Register our custom services
builder.Services.AddSingleton<IAIService, MockAIService>();
builder.Services.AddSingleton<ICacheService, MockCacheService>();
builder.Services.AddSingleton<IQueryFilterService, MockQueryFilterService>();
builder.Services.AddScoped<ISSEWriter, SSEWriter>();  // Register SSE Writer as scoped

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "AI Search API", 
        Version = "v1",
        Description = "API for AI-enhanced search with Server-Sent Events (SSE) support"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles(); // Add support for default files like index.html
app.UseStaticFiles(); // Add support for static files in wwwroot
app.UseAuthorization();
app.MapControllers();

app.Run();
