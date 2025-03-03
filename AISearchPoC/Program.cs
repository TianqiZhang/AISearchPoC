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
app.UseAuthorization();
app.MapControllers();

// Add a simple HTML page to test the SSE API
app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>AI Search API Demo</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }
        #response { white-space: pre-wrap; background: #f0f0f0; padding: 10px; border-radius: 5px; min-height: 100px; }
        input[type=""text""] { width: 70%; padding: 8px; }
        button { padding: 8px 15px; background: #0078d4; color: white; border: none; cursor: pointer; }
        .loading { color: #666; font-style: italic; }
    </style>
</head>
<body>
    <h1>AI Search API Demo</h1>
    <div>
        <input type=""text"" id=""queryInput"" placeholder=""Enter your search query..."">
        <button onclick=""performSearch()"">Search</button>
    </div>
    <div>
        <h3>Try these examples:</h3>
        <ul>
            <li><a href=""#"" onclick=""setQuery('dotnet')"">dotnet</a> (cached response)</li>
            <li><a href=""#"" onclick=""setQuery('sse')"">sse</a> (cached response)</li>
            <li><a href=""#"" onclick=""setQuery('What can you tell me about AI search?')"">What can you tell me about AI search?</a> (streaming response)</li>
            <li><a href=""#"" onclick=""setQuery('My password is 12345')"">My password is 12345</a> (unsuitable query)</li>
            <li><a href=""#"" onclick=""setQuery('Look up record with id 00000000-0000-0000-0000-000000000000')"">Look up record with id 00000000-0000-0000-0000-000000000000</a> (unsuitable query - GUID)</li>
        </ul>
    </div>
    <h2>Response:</h2>
    <div id=""response"">Enter a query and click Search to see results...</div>

    <script>
        let eventSource = null;

        function setQuery(query) {
            document.getElementById('queryInput').value = query;
            return false;
        }

        function performSearch() {
            const query = document.getElementById('queryInput').value.trim();
            
            if (!query) return;
            
            // Close any existing connection
            if (eventSource) {
                eventSource.close();
            }
            
            const responseElement = document.getElementById('response');
            responseElement.textContent = 'Loading...';
            responseElement.classList.add('loading');
            
            // Create a new EventSource connection
            eventSource = new EventSource(`/api/aisearch?q=${encodeURIComponent(query)}`);
            
            let fullResponse = '';
            
            // Handle incoming messages
            eventSource.onmessage = function(event) {
                responseElement.classList.remove('loading');
                const data = JSON.parse(event.data);
                
                if (data.status === 'no_ai') {
                    responseElement.textContent = `Query not suitable for AI: ${data.message}`;
                } 
                else if (data.status === 'cached') {
                    responseElement.textContent = `Cached response: ${data.ai_response}\n\nSources: ${data.sources.join(', ')}`;
                } 
                else if (data.status === 'stream') {
                    fullResponse += data.content;
                    responseElement.textContent = fullResponse;
                }
                else if (data.status === 'error') {
                    responseElement.textContent = `Error: ${data.message}`;
                }
            };
            
            // Handle the done event
            eventSource.addEventListener('done', function(event) {
                eventSource.close();
            });
            
            // Handle errors
            eventSource.onerror = function(event) {
                responseElement.textContent = 'Error: Connection to server failed.';
                eventSource.close();
            };
        }
    </script>
</body>
</html>
", "text/html"));

app.Run();
