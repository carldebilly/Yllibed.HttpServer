using Yllibed.HttpServer.Sse;

namespace Yllibed.HttpServer.Tests;

[TestClass]
public sealed class SseNegotiationFixture : FixtureBase
{
	private sealed class AcceptCheckedSseHandler : SseHandler
	{
		protected override bool ShouldHandle(IHttpServerRequest request, string relativePath)
			=> base.ShouldHandle(request, relativePath) && relativePath is "/accept";

		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			await sse.SendEventAsync("ok", ct: ct);
		}
 }

	[TestMethod]
	public async Task Sse_Returns406_When_Accept_DoesNotAllowEventStream()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse-accept");
		route.RegisterHandler(new AcceptCheckedSseHandler());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse-accept/accept");

		await using var conn = await SseTestClient.ConnectAsync(requestUri, CT, accept: "text/plain");
		conn.Response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
	}

	[TestMethod]
	public async Task Sse_Returns406_When_Accept_EventStream_Q0()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse-accept");
		route.RegisterHandler(new AcceptCheckedSseHandler());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse-accept/accept");

		await using var conn = await SseTestClient.ConnectAsync(requestUri, CT, accept: "text/event-stream;q=0");
		conn.Response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
	}

	[TestMethod]
	public async Task Sse_Returns406_When_Accept_TextWildcard_Q0()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse-accept");
		route.RegisterHandler(new AcceptCheckedSseHandler());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse-accept/accept");

		await using var conn = await SseTestClient.ConnectAsync(requestUri, CT, accept: "text/*;q=0");
		conn.Response.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
	}

	private sealed class LastEventIdEchoHandler : SseHandler
	{
		protected override bool ShouldHandle(IHttpServerRequest request, string relativePath)
			=> base.ShouldHandle(request, relativePath) && relativePath is "/echo";

		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			var lastId = sse.LastEventId ?? "<null>";
			await sse.SendEventAsync(lastId, eventName: "lastid", id: lastId, ct: ct);
		}
	}

	[TestMethod]
	public async Task Sse_LastEventId_IsExposed_ToHandler()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse-lastid");
		route.RegisterHandler(new LastEventIdEchoHandler());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse-lastid/echo");

		await using var conn = await SseTestClient.ConnectAsync(requestUri, CT, lastEventId: "42");
		conn.Response.StatusCode.Should().Be(HttpStatusCode.OK);

		SseTestClient.ServerSentEvent? first = null;
		await foreach (var ev in conn.ReadEventsAsync(CT))
		{
			first = ev;
			break;
		}

		first.Should().NotBeNull();
		first!.Event.Should().Be("lastid");
		first.Id.Should().Be("42");
		first.Data.Should().Be("42");
	}
}
