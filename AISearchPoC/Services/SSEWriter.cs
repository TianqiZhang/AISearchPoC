using System.Text.Json;

namespace AISearchPoC.Services;

public class SSEWriter : ISSEWriter
{
    private readonly JsonSerializerOptions _jsonOptions;
    private StreamWriter? _writer;

    public SSEWriter()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public StreamWriter Initialize(HttpResponse response)
    {
        // Prepare the response for SSE
        response.ContentType = "text/event-stream";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["Connection"] = "keep-alive";
        
        // Create a single StreamWriter for the entire response
        _writer = new StreamWriter(response.Body, leaveOpen: true)
        {
            AutoFlush = false // Important: We'll flush manually after each complete event
        };
        
        return _writer;
    }

    public async Task WriteEventAsync<T>(string eventType, T data, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        
        await _writer!.WriteLineAsync($"event: {eventType}");
        
        if (data is string stringData)
        {
            await _writer!.WriteLineAsync($"data: {stringData}");
        }
        else
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await _writer!.WriteLineAsync($"data: {json}");
        }
        
        await _writer!.WriteLineAsync(); // Empty line to signal end of event
        await _writer!.FlushAsync(); // Flush after completing each event
    }

    public async Task WriteDoneEventAsync(CancellationToken cancellationToken = default)
    {
        await WriteEventAsync("done", string.Empty, cancellationToken);
    }
    
    public async Task WriteErrorEventAsync(string errorMessage, CancellationToken cancellationToken = default)
    {
        await WriteEventAsync("message", new { status = "error", message = errorMessage }, cancellationToken);
        await WriteDoneEventAsync(cancellationToken);
    }
    
    private void EnsureInitialized()
    {
        if (_writer == null)
        {
            throw new InvalidOperationException("SSEWriter must be initialized with Initialize() before use.");
        }
    }
}