using Yllibed.HttpServer;
using Yllibed.HttpServer.Handlers;
using Yllibed.HttpServer.Sse;

// E2E = End-to-End. This small app is started by the E2E tests
// to spin up a real HTTP server and verify, via a real client, that the /ping endpoint
// responds correctly. Here we deliberately pick a dynamic port to showcase dynamic port discovery.
var server = new Server(); // dynamic port
server.RegisterHandler(new StaticHandler("ping", "text/plain", "pong"));

// Register an SSE endpoint under /sse/js that emits a single event
var sseRoute = new RelativePathHandler("sse");
sseRoute.RegisterHandler(new E2EApp.JsSseHandler());
server.RegisterHandler(sseRoute);

var (uri4, uri6) = server.Start();

// Expose the dynamically selected port in a machine-readable way for the Node E2E runner
Console.WriteLine("PORT={0}", uri4.Port);
Console.WriteLine($"E2EApp started on {uri4} and {uri6}");
Console.WriteLine("READY");

// Keep the app alive until killed
await Task.Delay(Timeout.InfiniteTimeSpan);

// Local SSE handler used only by the E2E app
namespace E2EApp
{
	internal sealed class JsSseHandler : SseHandler
	{
		protected override bool ShouldHandle(IHttpServerRequest request, string relativePath)
			=> base.ShouldHandle(request, relativePath) && relativePath == "/js";

		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			// Emit a simple event with a fixed id/name and JSON payload
   // Emit compact JSON manually to avoid extra project references
			await sse.SendEventAsync("{\"A\":1,\"B\":\"x\"}", eventName: "obj", id: "e2e-1", ct: ct).ConfigureAwait(false);
		}
	}
}
