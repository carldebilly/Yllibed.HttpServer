# Yllibed Http Server

A versatile, lightweight HTTP server for .NET applications. Self-contained with no dependencies on ASP.NET or other frameworks.

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

**Dynamic Port Assignment (Recommended):** Using port `0` automatically assigns an available port, preventing conflicts—perfect for testing, microservices, and team development.

```csharp
// ✅ Recommended approach
var server = new Server(); // Dynamic port
var (uri4, uri6) = server.Start();
var actualPort = new Uri(uri4).Port; // Get the assigned port
```

For advanced configuration, use `ServerOptions`:

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

## Limitations
* HTTP/1.1 only (no HTTP/2+ or WebSockets)
* No HTTPS/TLS support
* Designed for small-scale applications

For more examples and advanced usage, visit the [GitHub repository](https://github.com/carldebilly/Yllibed.HttpServer).
