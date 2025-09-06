# Yllibed Http Server – JSON Adapter

Helpers for building JSON endpoints and JSON SSE (Server‑Sent Events) with Yllibed.HttpServer, powered by Newtonsoft.Json.

## What it provides
- JsonHandlerBase<TResult>: base class to implement JSON endpoints quickly (sets content-type, serializes response, handles errors)
- Query string parsing helper built‑in to the base class (multi‑value aware)
- JSON serialization using Newtonsoft.Json
- SSE helpers: SendJsonAsync and SendJsonEventAsync to push compact JSON over text/event-stream

## Quick start – JSON endpoint
Implement a small handler that returns an object. The adapter serializes it to application/json and writes the proper status code.

```csharp
using Yllibed.HttpServer;
using Yllibed.HttpServer.Json;

public sealed class MyResult
{
    public string? A { get; set; }
    public string? B { get; set; }
}

public sealed class MyHandler : JsonHandlerBase<MyResult>
{
    public MyHandler() : base("GET", "/api/echo") { }

    protected override async Task<(MyResult result, ushort statusCode)> ProcessRequest(
        CancellationToken ct,
        string relativePath,
        IDictionary<string, string[]> query)
    {
        await Task.Yield();
        query.TryGetValue("a", out var a);
        query.TryGetValue("b", out var b);
        return (new MyResult { A = a?.FirstOrDefault(), B = b?.FirstOrDefault() }, 200);
    }
}

var server = new Server();
server.RegisterHandler(new MyHandler());
var (uri4, _) = server.Start();
Console.WriteLine(uri4 + "api/echo?a=1&b=2");
```

Response body:
```json
{
  "A": "1",
  "B": "2"
}
```

## Quick start – JSON over SSE
Send JSON directly as SSE data using the extension methods.

```csharp
using Yllibed.HttpServer.Sse;
using Yllibed.HttpServer.Json;

public sealed class PricesSse : SseHandler
{
    protected override bool ShouldHandle(IHttpServerRequest req, string path)
        => base.ShouldHandle(req, path) && path == "/prices";

    protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
    {
        await sse.SendJsonEventAsync("tick", new { Bid = 1.2345, Ask = 1.2347 }, id: "1", ct: ct);
    }
}
```

The JSON is serialized compact (no whitespace) and placed in the SSE data field.

## Behavior and details
- Content type: application/json for JsonHandlerBase responses
- Serializer: Newtonsoft.Json with Formatting.Indented for HTTP responses; Formatting.None for SSE
- Errors in your handler are caught and a 500 text/plain response is emitted. Log output uses Microsoft.Extensions.Logging via the server’s logger
- Paths can be passed with or without leading slash; base class normalizes them
- Method matching is case‑insensitive

## When to use
- Build small JSON APIs without bringing a full web framework
- Add real‑time JSON updates over SSE easily

## Package info
- Package: Yllibed.HttpServer.Json
- Depends on: Yllibed.HttpServer, Newtonsoft.Json
- Targets: see solution TargetFrameworks

## See also
For server setup, routing helpers, SSE basics, and DI, check the main project README: ../Yllibed.HttpServer/README.md or the repository root README.
