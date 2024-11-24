# Yllibed HttpServer
This is a versatile http server designed to be used in mobile/UWP applications.

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
    var myServer = new Server(8080); // create a web server on port 8080
    myServer.RegisterHandler(new StaticHandler("/", "text/plain", "Hello, world!")); // register a handler for the root
    var (uri4, uri6) = myServer.Start(); // start the server and get the URIs
   ```

## What it is
* Simple web server which can be extended using custom code
* No dependencies on ASP.NET or other frameworks, self-contained

## What it is not
* This HTTP server is not designed for performance or high capacity
* It's perfect for small applications, or small need, like to act as _return url_ for OAuth2 authentication using external browser.

## Limitations
* There is no support for HTTP 2.0+ (yet) or WebSockets
* There is no support for HTTPS (TLS)

## What you can do with it
* Use it for building a headless Windows IoT app (for SSDP discovery or simply end-user configuration)
* Use it for remote diagnostics/monitoring on your app

# Tips

## Opening port on Windows 10 IoT (typically on a Raspberry Pi)
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
