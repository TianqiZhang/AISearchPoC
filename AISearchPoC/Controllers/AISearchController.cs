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
    private readonly ISSEWriter _sseWriter;
    private readonly ILogger<AISearchController> _logger;

    public AISearchController(
        IAIService aiService,
        ICacheService cacheService,
        IQueryFilterService queryFilterService,
        ISSEWriter sseWriter,
        ILogger<AISearchController> logger)
    {
        _aiService = aiService;
        _cacheService = cacheService;
        _queryFilterService = queryFilterService;
        _sseWriter = sseWriter;
        _logger = logger;
    }

    [HttpGet]
    public async Task GetAIResponseAsync([FromQuery] string q, CancellationToken cancellationToken)
    {
        // Initialize the SSE writer with the current request's Response
        _sseWriter.Initialize(Response);

        try
        {
            // 1. Check if query is unsuitable for AI
            var (isSuitable, message) = await _queryFilterService.IsSuitableForAIAsync(q, cancellationToken);
            
            if (!isSuitable)
            {
                _logger.LogInformation("Query not suitable for AI: {Query}", q);
                await _sseWriter.WriteEventAsync("message", new { status = "no_ai", message }, cancellationToken);
                await _sseWriter.WriteDoneEventAsync(cancellationToken);
                return;
            }

            // 2. Check cache for existing response
            var cachedResponse = await _cacheService.GetCachedResponseAsync(q, cancellationToken);
            
            if (cachedResponse != null)
            {
                _logger.LogInformation("Found cached response for query: {Query}", q);
                await _sseWriter.WriteEventAsync("message", new 
                { 
                    status = "cached",
                    ai_response = cachedResponse.AIResponse,
                    sources = cachedResponse.Sources 
                }, cancellationToken);
                await _sseWriter.WriteDoneEventAsync(cancellationToken);
                return;
            }

            // 3. Stream AI-generated response
            _logger.LogInformation("Streaming AI response for query: {Query}", q);
            
            await foreach (var chunk in _aiService.GetAIResponseStreamAsync(q, cancellationToken))
            {
                await _sseWriter.WriteEventAsync("message", new { status = "stream", content = chunk }, cancellationToken);
                
                // Stop streaming if client disconnects
                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            // Send the done event to signal end of stream
            if (!cancellationToken.IsCancellationRequested)
            {
                await _sseWriter.WriteDoneEventAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected or request canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AI search request");
            await _sseWriter.WriteErrorEventAsync("An error occurred processing your request", cancellationToken);
        }
    }
}