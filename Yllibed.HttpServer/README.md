# Yllibed Http Server

A versatile, lightweight HTTP server for .NET applications. Self-contained with no ASP.NET dependency. Ideal for tools, local services, test harnesses, IoT and desktop apps.

## Features
- Single-assembly, minimal footprint, no external web framework
- Plug-in handler model: register one or many handlers; first to respond wins
- IPv4 and IPv6 support; returns both URIs from Start()
- Dynamic port assignment (Port = 0) to avoid conflicts (recommended)
- Microsoft.Extensions.DependencyInjection integration and IOptions<ServerOptions>
- Server-Sent Events (SSE) helper base class for real-time event streams
- Static content responses and simple REST-style endpoints
- Runs on .NET (see package TargetFrameworks)

## Quick Start

```csharp
// Recommended: Use dynamic port assignment
var server = new Server(); // Port 0 by default - no conflicts!
server.RegisterHandler(new StaticHandler("/", "text/plain", "Hello, world!"));
var (uri4, uri6) = server.Start();
Console.WriteLine($"Server running at: {uri4}");
```

Or with a fixed port:
```csharp
var server = new Server(8080);
server.RegisterHandler(new StaticHandler("/", "text/plain", "Hello, world!"));
var (uri4, uri6) = server.Start();
```

## Common Use Cases
* OAuth2 return URL endpoints
* Remote diagnostics/monitoring
* IoT device configuration interfaces
* Simple REST API endpoints

## Configuration

Dynamic port assignment (recommended): using port 0 automatically selects a free TCP port, preventing conflicts—perfect for tests, parallel runs and local tools.

```csharp
// ✅ Recommended approach
var server = new Server(); // Port 0 by default
var (uri4, uri6) = server.Start();
var actualPort = new Uri(uri4).Port; // Discover the assigned port
```

For advanced configuration, use ServerOptions:

```csharp
var serverOptions = new ServerOptions
{
    Port = 0, // Dynamic port (recommended)
    Hostname4 = "127.0.0.1",
    Hostname6 = "::1",
    BindAddress4 = IPAddress.Any // Listen on all interfaces
};
var server = new Server(serverOptions);
```

## Dependency Injection

Works with Microsoft.Extensions.DependencyInjection:

```csharp
// Option 1: Extension method (cleanest) - uses dynamic ports by default
services.AddYllibedHttpServer(); // Zero configuration, no conflicts!

// Or configure explicitly:
services.AddYllibedHttpServer(opts =>
{
    opts.Port = 0; // Dynamic port (recommended)
    opts.Hostname4 = "127.0.0.1";
});

// Option 2: Configure + AddSingleton
services.Configure<ServerOptions>(opts => { opts.Port = 0; });
services.AddSingleton<Server>(); // Auto-selects IOptions<> constructor
```

## Handlers and Routing
- Handlers are small classes implementing IHttpHandler. You can register multiple handlers; they are queried in order and the first one to produce a response wins.
- Use RelativePathHandler to compose simple routing trees under a base path.

Example:
```csharp
var server = new Server();
var api = new RelativePathHandler("/api");
api.RegisterHandler(new StaticHandler("/ping", "text/plain", "pong"));
server.RegisterHandler(api);
server.Start();
```

## Server-Sent Events (SSE)
Stream real-time events over HTTP using the SseHandler base class or StartSseSession extension.

```csharp
public sealed class MySseHandler : SseHandler
{
    protected override bool ShouldHandle(IHttpServerRequest req, string path)
        => base.ShouldHandle(req, path) && path == "/sse";

    protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
    {
        for (var i = 0; i < 5 && !ct.IsCancellationRequested; i++)
        {
            await sse.SendEventAsync($"tick {i}", eventName: "tick", id: i.ToString(), ct: ct);
            await Task.Delay(1000, ct);
        }
    }
}

var root = new RelativePathHandler("/");
root.RegisterHandler(new MySseHandler());
server.RegisterHandler(root);
```

## Design goals
- Keep things tiny and dependency-free
- Prefer clarity over features; you own the control flow in your handlers
- Make local and internal scenarios painless (dynamic ports, simple DI)

## Limitations
* HTTP/1.1 only (no HTTP/2+ or WebSockets)
* No HTTPS/TLS support
* Designed for small-scale applications

For more examples and advanced usage, visit the GitHub repository: https://github.com/carldebilly/Yllibed.HttpServer
