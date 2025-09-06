using System.Globalization;
using Yllibed.HttpServer.Sse;

namespace Yllibed.HttpServer.Tests;

[TestClass]
public sealed class SseFixture : FixtureBase
{
	private sealed class TestSseHandler : SseHandler
	{
		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions
			{
				AutoFlush = true,
				HeartbeatInterval = TimeSpan.Zero // keep test deterministic
			};

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			await sse.SendCommentAsync("start", ct);
			await sse.SendEventAsync("hello\nworld", eventName: "greet", id: "1", ct: ct);
			await sse.SendEventAsync("bye", id: "2", ct: ct);
		}
	}

	[TestMethod]
	public async Task Sse_BasicEvents_AreReceived()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse");
		route.RegisterHandler(new TestSseHandler());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse");

		await using var conn = await SseTestClient.ConnectAsync(requestUri, CT);
		var resp = conn.Response;
		resp.StatusCode.Should().Be(HttpStatusCode.OK);
		resp.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
		resp.Headers.CacheControl.Should().NotBeNull();
		resp.Headers.CacheControl!.NoCache.Should().BeTrue();

		var received = new List<SseTestClient.ServerSentEvent>();
		await foreach (var ev in conn.ReadEventsAsync(CT))
		{
			received.Add(ev);
			if (received.Count >= 2) break; // we expect two events then end the session
		}

		received.Count.Should().Be(2);
		received[0].Event.Should().Be("greet");
		received[0].Id.Should().Be("1");
		received[0].Data.Should().Be("hello\nworld");

		received[1].Event.Should().BeNull(); // default event name is 'message' but we keep null in helper
		received[1].Id.Should().Be("2");
		received[1].Data.Should().Be("bye");
	}
}


[TestClass]
public sealed class SseLifecycleFixture : FixtureBase
{
	private sealed class TestSseHandler2 : SseHandler
	{
		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			await sse.SendEventAsync("hello\nworld", eventName: "greet", id: "1", ct: ct);
			await sse.SendEventAsync("bye", id: "2", ct: ct);
		}
	}

	[TestMethod]
	public async Task Sse_StreamEnds_WhenHandlerCompletes()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse2");
		route.RegisterHandler(new TestSseHandler2());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse2");

		await using var conn = await SseTestClient.ConnectAsync(requestUri, CT);
		var resp = conn.Response;
		resp.StatusCode.Should().Be(HttpStatusCode.OK);
		resp.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

		var received = new List<SseTestClient.ServerSentEvent>();
		await foreach (var ev in conn.ReadEventsAsync(CT))
		{
			received.Add(ev);
		}

		received.Count.Should().Be(2, "stream should close when handler completes after sending two events");
	}

	private sealed class LoopingSseHandler : SseHandler
	{
		private readonly TaskCompletionSource<bool> _disconnected;
		public LoopingSseHandler(TaskCompletionSource<bool> disconnected) => _disconnected = disconnected;

		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			try
			{
				var i = 0;
				while (!ct.IsCancellationRequested)
				{
					var idStr = i.ToString(CultureInfo.InvariantCulture);
					await sse.SendEventAsync("tick-" + idStr, eventName: "tick", id: idStr, ct: ct);
					await Task.Delay(10, ct);
					i++;
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when client disconnects or server cancels
			}
			finally
			{
				_disconnected.TrySetResult(true);
			}
		}
	}

	[TestMethod]
	public async Task Sse_HandlerCancels_WhenClientDisconnects()
	{
		using var sut = new Server();
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var route = new RelativePathHandler("sse3");
		route.RegisterHandler(new LoopingSseHandler(tcs));
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "sse3");

		await using (var conn = await SseTestClient.ConnectAsync(requestUri, CT))
		{
			conn.Response.StatusCode.Should().Be(HttpStatusCode.OK);
			// Read a single event then disconnect
			await foreach (var _ in conn.ReadEventsAsync(CT))
			{
				break;
			}
			// Disposing the connection should close the TCP stream
		}

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
		completed.Should().Be(tcs.Task, "handler should complete when client disconnects");
		( await tcs.Task ).Should().BeTrue();
	}
}


[TestClass]
public sealed class SseMultiHandlersFixture : FixtureBase
{
	private sealed class SseHandlerA : SseHandler
	{
		protected override bool ShouldHandle(IHttpServerRequest request, string relativePath)
			=> base.ShouldHandle(request, relativePath)
		   	   && string.Equals(relativePath, "/a", StringComparison.Ordinal);

		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			await sse.SendEventAsync("a1", eventName: "alpha", id: "A", ct: ct);
		}
	}

	private sealed class SseHandlerB : SseHandler
	{
		protected override bool ShouldHandle(IHttpServerRequest request, string relativePath)
			=> base.ShouldHandle(request, relativePath)
		   	   && string.Equals(relativePath, "/b", StringComparison.Ordinal);

		protected override SseOptions? GetOptions(IHttpServerRequest request, string relativePath)
			=> new SseOptions { AutoFlush = true, HeartbeatInterval = TimeSpan.Zero };

		protected override async Task HandleSseSession(ISseSession sse, CancellationToken ct)
		{
			await sse.SendEventAsync("b1", eventName: "beta", id: "B", ct: ct);
		}
	}

	[TestMethod]
	public async Task Sse_MultipleHandlers_DifferentStreams()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("sse-multi");
		route.RegisterHandler(new SseHandlerA());
		route.RegisterHandler(new SseHandlerB());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var uriA = new Uri(uri4, "sse-multi/a");
		var uriB = new Uri(uri4, "sse-multi/b");

		await using var connA = await SseTestClient.ConnectAsync(uriA, CT);
		await using var connB = await SseTestClient.ConnectAsync(uriB, CT);

		connA.Response.StatusCode.Should().Be(HttpStatusCode.OK);
		connB.Response.StatusCode.Should().Be(HttpStatusCode.OK);
		connA.Response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
		connB.Response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

		SseTestClient.ServerSentEvent? firstA = null;
		SseTestClient.ServerSentEvent? firstB = null;

		await foreach (var ev in connA.ReadEventsAsync(CT))
		{
			firstA = ev;
			break;
		}
		await foreach (var ev in connB.ReadEventsAsync(CT))
		{
			firstB = ev;
			break;
		}

		firstA.Should().NotBeNull();
		firstB.Should().NotBeNull();
		firstA!.Event.Should().Be("alpha");
		firstA.Id.Should().Be("A");
		firstA.Data.Should().Be("a1");
		firstB!.Event.Should().Be("beta");
		firstB.Id.Should().Be("B");
		firstB.Data.Should().Be("b1");
	}
}
