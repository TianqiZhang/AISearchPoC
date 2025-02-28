using System.Text.Json;
using AISearchPoC.Services;
using Microsoft.AspNetCore.Mvc;

namespace AISearchPoC.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AISearchController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly ICacheService _cacheService;
    private readonly IQueryFilterService _queryFilterService;
    private readonly ILogger<AISearchController> _logger;

    public AISearchController(
        IAIService aiService,
        ICacheService cacheService,
        IQueryFilterService queryFilterService,
        ILogger<AISearchController> logger)
    {
        _aiService = aiService;
        _cacheService = cacheService;
        _queryFilterService = queryFilterService;
        _logger = logger;
    }

    [HttpGet]
    public async Task GetAIResponseAsync([FromQuery] string q, CancellationToken cancellationToken)
    {
        // Set response content type for Server-Sent Events
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        // Get a StreamWriter to write the SSE data
        var writer = new StreamWriter(Response.Body);

        try
        {
            // 1. Check if query is unsuitable for AI
            var (isSuitable, message) = await _queryFilterService.IsSuitableForAIAsync(q, cancellationToken);
            
            if (!isSuitable)
            {
                _logger.LogInformation("Query not suitable for AI: {Query}", q);
                await WriteSSEEventAsync(writer, "message", new { status = "no_ai", message });
                await WriteSSEEventAsync(writer, "done", string.Empty);
                return;
            }

            // 2. Check cache for existing response
            var cachedResponse = await _cacheService.GetCachedResponseAsync(q, cancellationToken);
            
            if (cachedResponse != null)
            {
                _logger.LogInformation("Found cached response for query: {Query}", q);
                await WriteSSEEventAsync(writer, "message", new 
                { 
                    status = "cached",
                    ai_response = cachedResponse.AIResponse,
                    sources = cachedResponse.Sources 
                });
                await WriteSSEEventAsync(writer, "done", string.Empty);
                return;
            }

            // 3. Stream AI-generated response
            _logger.LogInformation("Streaming AI response for query: {Query}", q);
            
            await foreach (var chunk in _aiService.GetAIResponseStreamAsync(q, cancellationToken))
            {
                await WriteSSEEventAsync(writer, "message", new { status = "stream", content = chunk });
                
                // Stop streaming if client disconnects
                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            // Send the done event to signal end of stream
            if (!cancellationToken.IsCancellationRequested)
            {
                await WriteSSEEventAsync(writer, "done", string.Empty);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected or request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI search request");
            try
            {
                await WriteSSEEventAsync(writer, "message", new { status = "error", message = "An error occurred processing your request" });
                await WriteSSEEventAsync(writer, "done", string.Empty);
            }
            catch
            {
                // Best effort to send error message, ignore if it fails
            }
        }
    }

    private async Task WriteSSEEventAsync(StreamWriter writer, string eventType, object data)
    {
        await writer.WriteLineAsync($"event: {eventType}");
        
        if (data is string stringData)
        {
            await writer.WriteLineAsync($"data: {stringData}");
        }
        else
        {
            var json = JsonSerializer.Serialize(data);
            await writer.WriteLineAsync($"data: {json}");
        }
        
        await writer.WriteLineAsync(); // Empty line to signal end of event
        await writer.FlushAsync();
    }
}