using System.Runtime.CompilerServices;

namespace AISearchPoC.Services;

// Mock implementation of AI Service - in production, this would connect to an actual AI model
public class MockAIService : IAIService
{
    private readonly Random _random = new();

    public async IAsyncEnumerable<string> GetAIResponseStreamAsync(string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simulate AI generation with delays between chunks
        var words = query.Split(' ');
        
        foreach (var word in words)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            // Simulate processing time for each chunk
            await Task.Delay(_random.Next(100, 500), cancellationToken);
            
            // Return a simple response that includes the original word
            yield return $"{word} is an interesting term. ";
        }
        
        // Final summary
        if (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_random.Next(300, 800), cancellationToken);
            yield return $"\nYour query was: '{query}'. This is a simulated AI response.";
        }
    }
}

// Mock implementation of Cache Service - in production, this would use Redis or another cache solution
public class MockCacheService : ICacheService
{
    private readonly Dictionary<string, CachedResponse> _cache = new(StringComparer.OrdinalIgnoreCase)
    {
        // Pre-populate with some example cached responses
        { "dotnet", new CachedResponse 
            { 
                AIResponse = ".NET is a free, open-source development platform maintained by Microsoft.", 
                Sources = new List<string> { "https://dotnet.microsoft.com/", "https://docs.microsoft.com" } 
            }
        },
        { "sse", new CachedResponse 
            { 
                AIResponse = "Server-Sent Events (SSE) is a server push technology enabling a client to receive automatic updates from a server via HTTP connection.", 
                Sources = new List<string> { "https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events" } 
            }
        }
    };

    public Task<CachedResponse?> GetCachedResponseAsync(string query, CancellationToken cancellationToken = default)
    {
        // Check if we have an exact match in our cache
        if (_cache.TryGetValue(query.Trim(), out var cachedResponse))
        {
            return Task.FromResult<CachedResponse?>(cachedResponse);
        }

        return Task.FromResult<CachedResponse?>(null);
    }
}

// Mock implementation of Query Filter Service - in production, this would use more sophisticated logic
public class MockQueryFilterService : IQueryFilterService
{
    private readonly List<string> _sensitivePatterns = new()
    {
        "password",
        "secret",
        "token",
        "api key",
        "social security",
        "credit card",
    };

    public Task<(bool IsSuitable, string? Message)> IsSuitableForAIAsync(string query, CancellationToken cancellationToken = default)
    {
        // Simple check for GUID-like patterns
        if (query.Contains(Guid.Empty.ToString().Substring(0, 8), StringComparison.OrdinalIgnoreCase) ||
            query.Split(' ').Any(word => Guid.TryParse(word, out _)))
        {
            // Fix: Explicitly return a nullable string for the Message
            string message = "Query appears to contain a GUID which is not suitable for AI processing.";
            return Task.FromResult((false, (string?)message));
        }

        // Check for sensitive information patterns
        foreach (var pattern in _sensitivePatterns)
        {
            if (query.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                // Fix: Explicitly return a nullable string for the Message
                string message = $"Query potentially contains sensitive information ({pattern}).";
                return Task.FromResult((false, (string?)message));
            }
        }

        // Query passes all checks
        return Task.FromResult((true, (string?)null));
    }
}