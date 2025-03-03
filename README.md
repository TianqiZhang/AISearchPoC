# AI Search API with Server-Sent Events (SSE)

This project demonstrates a proof of concept for an AI-enhanced search API that uses Server-Sent Events (SSE) for real-time streaming of AI-generated responses.

## 🔍 Overview

The AI Search API enhances traditional search by integrating AI-generated responses. Different queries are handled in different ways:

- **Filter Unsuitable Queries**: Certain queries (containing sensitive information, GUIDs) are rejected with an explanation
- **Cached Responses**: Common queries return pre-cached responses for immediate results
- **Streaming AI Generation**: Other queries receive real-time, progressively streamed AI-generated responses

To ensure a consistent and smooth user experience, the API always uses Server-Sent Events (SSE), regardless of whether the response is immediate or streamed.

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) or later

### Running the Application

1. Clone the repository
2. Navigate to the project directory:
   ```
   cd AISearchPoC
   ```
3. Run the application:
   ```
   dotnet run
   ```
4. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000` (the port may vary)
5. Use the web interface to test the API with different queries

## 🧩 Project Structure

- **Controllers**: Contains the `AISearchController` which handles API requests and manages SSE responses
- **Services**: Contains service interfaces and their implementations:
  - `IAIService`: Service for generating AI responses
  - `ICacheService`: Service for managing cached responses
  - `IQueryFilterService`: Service for filtering unsuitable queries
  - `ISSEWriter`: Helper for formatting and writing Server-Sent Events

## 🔄 API Behavior

The API handles queries in three main ways:

### Case 1: Query Not Suitable for AI
When a query contains sensitive information (passwords, secrets, GUIDs, etc.):
```
event: message
data: {"status": "no_ai", "message": "Query is unsuitable for AI processing"}
event: done
```

### Case 2: AI Answer is Cached
When a query has a pre-cached response:
```
event: message
data: {"status": "cached", "ai_response": "Here is the cached AI answer.", "sources": ["https://docs.microsoft.com"]}
event: done
```

### Case 3: AI Answer Needs Generation
When a query requires real-time AI generation:
```
event: message
data: {"status": "stream", "content": "AI-generated answer part 1"}

event: message
data: {"status": "stream", "content": "AI-generated answer part 2"}

event: done
```

## 💻 Demo UI

The application includes a simple HTML page to test the SSE API with example queries:
- **Cached response examples**: "dotnet", "sse"
- **Streaming response example**: "What can you tell me about AI search?"
- **Unsuitable query examples**: "My password is 12345", "Look up record with id 00000000-0000-0000-0000-000000000000"

## 🧪 Implementation Details

This project is a proof of concept and uses these implementations:

- **MockAIService**: Simulates streaming AI responses with delays
- **MockCacheService**: Provides pre-defined cached responses for certain queries
- **MockQueryFilterService**: Checks for sensitive information patterns in queries
- **SSEWriter**: Provides a clean abstraction for writing Server-Sent Events

The SSEWriter abstraction allows for clean separation of concerns:
- It's registered as a scoped service to maintain state during request processing
- It handles all SSE-specific formatting and protocol details
- It properly flushes the response stream after each event
- It can be easily reused across controllers or projects

In a production environment, the mock services would be replaced with:
- Real AI model integration (e.g., OpenAI, Azure AI)
- Proper caching solution (e.g., Redis)
- More sophisticated query filtering and PII detection

## 📝 Future Enhancements

- Implement proper AI service integration
- Add persistent caching with expiration policies
- Enhance query filtering with ML-based PII detection
- Add authentication and rate limiting
- Implement client reconnection handling for SSE
- Add analytics for query patterns and response quality

## 📚 Resources

For more information on the technologies used:
- [Server-Sent Events (MDN)](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)
- [ASP.NET Core Web API](https://docs.microsoft.com/en-us/aspnet/core/web-api)
- [C# IAsyncEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.iasyncenumerable-1)