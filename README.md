# Yllibed HttpServer

This is a versatile http server designed to be used in mobile/UWP applications and any applications which need to expose a simple web server.

## Packages and NuGet Statistics

| Package                                                                                   | Downloads                                                                                      | Stable Version                                                                                         | Pre-release Version                                                                                     |
|-------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| [**HttpServer**](https://www.nuget.org/packages/Yllibed.HttpServer/)                      | ![Downloads](https://img.shields.io/nuget/dt/Yllibed.HttpServer?label=Downloads)              | ![Stable](https://img.shields.io/nuget/v/Yllibed.HttpServer?label=Stable&labelColor=blue)              | ![Pre-release](https://img.shields.io/nuget/vpre/Yllibed.HttpServer?label=Pre-release&labelColor=yellow)           |
| [**HttpServer.Json**](https://www.nuget.org/packages/Yllibed.HttpServer.Json/)            | ![Downloads](https://img.shields.io/nuget/dt/Yllibed.HttpServer.Json?label=Downloads)         | ![Stable](https://img.shields.io/nuget/v/Yllibed.HttpServer.Json?label=Stable&labelColor=blue)         | ![Pre-release](https://img.shields.io/nuget/vpre/Yllibed.HttpServer.Json?label=Pre-release&labelColor=yellow)       |

## Quick start-up

1. First install nuget package:
	```shell
		PM> Install-Package Yllibed.HttpServer 
	```

2. Register a server in your app:
   ```csharp
    var myServer = new Server(); // Uses dynamic port (recommended)
    myServer.RegisterHandler(new StaticHandler("/", "text/plain", "Hello, world!")); 
    var (uri4, uri6) = myServer.Start(); // Get the actual URIs with assigned ports
    
    Console.WriteLine($"Server running on: {uri4}");
   ```

   Or specify a fixed port:
   ```csharp
    var myServer = new Server(8080); // Fixed port
    myServer.RegisterHandler(new StaticHandler("/", "text/plain", "Hello, world!"));
    var (uri4, uri6) = myServer.Start();
   ```

   Or with dependency injection and configuration:
   ```csharp
    var services = new ServiceCollection();
    services.AddYllibedHttpServer(opts =>
    {
        opts.Port = 0; // Dynamic port (recommended)
        opts.Hostname4 = "127.0.0.1";
        opts.Hostname6 = "::1";
    });
    
    var sp = services.BuildServiceProvider();
    var server = sp.GetRequiredService<Server>();
    server.RegisterHandler(new StaticHandler("/", "text/plain", "Hello, world!"));
    var (uri4, uri6) = server.Start();
   ```

## What it is
* Simple web server which can be extended using custom code
* No dependencies on ASP.NET or other frameworks, self-contained

## What it is not
* This HTTP server is not designed for performance or high capacity
* It's perfect for small applications, or small need, like to act as _return url_ for OAuth2 authentication using external browser.

## Features
* Simple, lightweight, self-contained HTTP server
* Supports IPv4 and IPv6
* Supports HTTP 1.1 (limited: no keep-alive, no chunked encoding)
* Supports GET, POST, PUT, DELETE, HEAD, OPTIONS, TRACE, PATCH - even custom methods
* Supports static files
* Supports custom headers
* Supports custom status codes
* Supports custom content types
* Supports custom content encodings
* Supports dependency injection and configuration via `IOptions<ServerOptions>`
* Configurable bind addresses and hostnames for IPv4/IPv6
* Supports dynamic port assignment

## Common use cases
* Return URL for OAuth2 authentication using external browser
* Remote diagnostics/monitoring on your app
* Building a headless Windows IoT app (for SSDP discovery or simply end-user configuration)
* Any other use case where you need to expose a simple web server

## Limitations
* There is no support for HTTP 2.0+ (yet) or WebSockets
* There is no support for HTTPS (TLS)

## Security and Intended Use (No TLS)
This server uses plain HTTP with no transport encryption. It is primarily intended for:
- Localhost usage (loopback) during development or as an OAuth redirect target.
- Internal communication on trusted networks, e.g., between Docker containers on the same host or in a private overlay network.
- Embedded/local scenarios (IoT, diagnostics) where transport security is handled elsewhere.

If you need to expose it on a public or untrusted network:
- Put it behind a reverse proxy that terminates TLS (e.g., Nginx, Traefik, Caddy, IIS/ASP.NET Core reverse proxy) and forward to this server over HTTP on a private interface.
- Alternatively, use a secure tunnel (SSH, Cloudflare Tunnel, etc.).
- Bind to loopback only (127.0.0.1 / ::1) when you want to ensure local-only access.

Note: Authentication/authorization is not built-in; implement it in your handlers or at the proxy layer as needed.

## Configuration and Dependency Injection

The server can now be configured via a `ServerOptions` POCO. You can construct `Server` directly with a `ServerOptions` instance, or register it with Microsoft.Extensions.DependencyInjection using `IOptions<ServerOptions>`.

### ServerOptions Properties

* `Port` - Port number to listen to (0 = dynamic)
* `BindAddress4` - IPv4 address to bind the listener to (defaults to `IPAddress.Any`)
* `BindAddress6` - IPv6 address to bind the listener to (defaults to `IPAddress.IPv6Any`)
* `Hostname4` - Hostname used to compose the public IPv4 URI (defaults to "127.0.0.1")
* `Hostname6` - Hostname used to compose the public IPv6 URI (defaults to "::1")

### Dynamic Port Assignment (Recommended)

Using port `0` enables **dynamic port assignment**, which is the **recommended approach** for most applications:

**Key Advantages:**
- **Zero port conflicts**: The operating system automatically assigns an available port
- **Perfect for testing**: Multiple test instances can run simultaneously without conflicts
- **Microservices architecture**: Each service gets its own unique port automatically
- **Team collaboration**: No need to coordinate port assignments between developers
- **CI/CD friendly**: Parallel builds and tests work seamlessly

**Best Practices with Dynamic Ports:**

```csharp
// ✅ Recommended: Use dynamic ports (default behavior)
var server = new Server(); // Port 0 by default
server.RegisterHandler(new StaticHandler("/", "text/plain", "Hello!"));
var (uri4, uri6) = server.Start();

// Capture the actual assigned port for logging or service discovery
var actualPort = new Uri(uri4).Port;
Console.WriteLine($"Server started on port: {actualPort}");
Console.WriteLine($"Access your service at: {uri4}");

// For service registration with discovery systems
await RegisterWithServiceDiscovery(uri4);
```

**In production with Dependency Injection:**

```csharp
services.AddYllibedHttpServer(); // Uses port 0 by default - no conflicts!

// Or be explicit for documentation purposes:
services.AddYllibedHttpServer(opts =>
{
    opts.Port = 0; // Dynamic port - recommended
    opts.BindAddress4 = IPAddress.Any; // Accept from all interfaces
    opts.Hostname4 = Environment.MachineName; // Use machine name in URIs
});
```

**When NOT to use dynamic ports:**
- Public-facing web services requiring well-known ports (80, 443)
- Legacy systems expecting fixed ports
- Load balancer configurations requiring static endpoints
- Development scenarios where you need predictable URLs

### Basic Configuration Example

```csharp
var serverOptions = new ServerOptions
{
    Port = 5000,
    Hostname4 = "192.168.1.100", // Custom hostname for callbacks
    BindAddress4 = IPAddress.Any  // Listen on all interfaces
};

var server = new Server(serverOptions);
server.Start();
```

### Dependency Injection Example

The cleanest approach using extension methods:

```csharp
var services = new ServiceCollection();
services.AddYllibedHttpServer(opts =>
{
    opts.Port = 5000;
    opts.Hostname4 = "127.0.0.1";
    opts.Hostname6 = "::1";
    opts.BindAddress4 = IPAddress.Parse("0.0.0.0");
});

var sp = services.BuildServiceProvider();
var server = sp.GetRequiredService<Server>();
server.Start();
```

Alternative using `services.Configure<>()` and automatic constructor selection:

```csharp
var services = new ServiceCollection();
services.Configure<ServerOptions>(opts =>
{
    opts.Port = 5000;
    opts.Hostname4 = "127.0.0.1";
    opts.Hostname6 = "::1";
});
services.AddSingleton<Server>(); // Uses IOptions<ServerOptions> constructor automatically

var sp = services.BuildServiceProvider();
var server = sp.GetRequiredService<Server>();
server.Start();
```

This allows you to control the bind addresses and hostnames used to compose the public URIs that the server logs, which is especially useful for OAuth callbacks and REST API applications.

## Tips

### Opening port on Windows 10 IoT (typically on a Raspberry Pi)
If you want to open "any" port on a Raspberry Pi running Windows 10 IoT, you may
need to open a port.

1. First, connect to your device using powershell:
   ```shell
      Enter-PsSession -ComputerName <device name or ip> -Credential .\Administrator
   ```
2. Add a rule in the firewall to authorize inbound traffic to your application: (example for port 8080)
   ```shell
      netsh advfirewall firewall add rule name="My Application Webserver" dir=in action=allow protocol=TCP localport=8080
   ```

### OAuth2 Callback Configuration
When using this server for OAuth2 callbacks, dynamic ports are especially useful:

```csharp
services.AddYllibedHttpServer(); // Dynamic port prevents conflicts

var server = serviceProvider.GetRequiredService<Server>();
server.RegisterHandler(new MyOAuthCallbackHandler());
var (uri4, uri6) = server.Start();

// Use the actual URI for OAuth redirect registration
var redirectUri = $"{uri4}/oauth/callback";
Console.WriteLine($"Register this redirect URI with your OAuth provider: {redirectUri}");

// No port conflicts even if multiple OAuth flows run simultaneously!
```

For scenarios requiring fixed callback URLs:
```csharp
services.Configure<ServerOptions>(opts =>
{
    opts.Port = 5001;
    opts.Hostname4 = "127.0.0.1"; // Match your OAuth redirect URI
    opts.BindAddress4 = IPAddress.Loopback; // Only accept local connections
});
```

### Listening on All Interfaces
To accept connections from other machines on your network:

```csharp
var serverOptions = new ServerOptions
{
    Port = 8080,
    BindAddress4 = IPAddress.Any,        // Listen on all IPv4 interfaces
    BindAddress6 = IPAddress.IPv6Any,    // Listen on all IPv6 interfaces
    Hostname4 = "192.168.1.100",        // Your actual IP for public URIs
    Hostname6 = "::1"                   // IPv6 loopback
};
```
