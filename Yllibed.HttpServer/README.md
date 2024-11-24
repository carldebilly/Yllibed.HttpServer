# Yllibed Http Server

This is a versatile http server designed to be used any applications which need to expose a simple web server.

There is no dependencies on ASP.NET or other frameworks, it is self-contained.

Common use cases are:
* Return URL for OAuth2 authentication using external browser
* Remote diagnostics/monitoring on your app
* Building a headless Windows IoT app (for SSDP discovery or simply end-user configuration)
* Any other use case where you need to expose a simple web server

Limitations:
* There is no support for HTTP 2.0+ (yet) or WebSockets
* There is no support for HTTPS (TLS)
* This HTTP server is not designed for performance or high capacity
* It's perfect for small applications, or small need
