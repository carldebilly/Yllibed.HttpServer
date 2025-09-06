using System.Text;
using System.Threading.Tasks;
using System.IO;
using Yllibed.HttpServer.Handlers;
using Yllibed.HttpServer.Tests;

namespace Yllibed.HttpServer.Json.Tests;

[TestClass]
public sealed class SseJsonFixture : FixtureBase
{
	private sealed class JsonSseHandler : SseHandler
	{
		protected override bool ShouldHandle(IHttpServerRequest request, string relativePath)
			=> base.ShouldHandle(request, relativePath) && relativePath is "/js";

		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			var payload = new { A = 1, B = "x" };
			await sse.SendJsonEventAsync("obj", payload, id: "j1", ct: ct);
		}
	}

	[TestMethod]
	public async Task Sse_SendJson_WritesCompactJsonInData()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse-json");
		route.RegisterHandler(new JsonSseHandler());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse-json/js");

		await using var conn = await SseTestClient.ConnectAsync(requestUri, CT);
		conn.Response.StatusCode.Should().Be(HttpStatusCode.OK);
		conn.Response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

		SseTestClient.ServerSentEvent? first = null;
		await foreach (var ev in conn.ReadEventsAsync(CT))
		{
			first = ev;
			break;
		}
		first.Should().NotBeNull();
		first!.Event.Should().Be("obj");
		first.Id.Should().Be("j1");
		first.Data.Should().Be("""{"A":1,"B":"x"}""");
	}
}
