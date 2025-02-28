using System.Runtime.CompilerServices;

namespace AISearchPoC.Services;

// Interface for AI response generation service
public interface IAIService
{
    // Returns an async stream of AI response chunks for a given query
    IAsyncEnumerable<string> GetAIResponseStreamAsync(string query, CancellationToken cancellationToken = default);
}

// Interface for caching service
public interface ICacheService
{
    // Checks if a query has a cached response and returns it if available
    Task<CachedResponse?> GetCachedResponseAsync(string query, CancellationToken cancellationToken = default);
}

// Interface for query validation/filtering service
public interface IQueryFilterService
{
    // Checks if a query is suitable for AI processing (no secrets, PII, etc.)
    Task<(bool IsSuitable, string? Message)> IsSuitableForAIAsync(string query, CancellationToken cancellationToken = default);
}

// Model for cached responses
public class CachedResponse
{
    public string AIResponse { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new List<string>();
}