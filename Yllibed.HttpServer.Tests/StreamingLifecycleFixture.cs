namespace Yllibed.HttpServer.Tests;

[TestClass]
public sealed class StreamingLifecycleFixture : FixtureBase
{
	private sealed class FiniteStreamingHandler : IHttpHandler
	{
		public Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
		{
			if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(relativePath, "/finite", StringComparison.Ordinal))
			{
				return Task.CompletedTask;
			}

			request.SetStreamingResponse("text/plain", async (writer, wct) =>
			{
				for (var i = 0; i < 5; i++)
				{
					await writer.WriteLineAsync("line-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
					await writer.FlushAsync(wct).ConfigureAwait(false);
				}
			}, headers: null);

			return Task.CompletedTask;
		}
	}

	[TestMethod]
	public async Task Streaming_StreamEnds_WhenWriterCompletes()
	{
		using var sut = new Server();
		var route = new RelativePathHandler("stream");
		route.RegisterHandler(new FiniteStreamingHandler());
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "stream/finite");

		using var client = new HttpClient();
		using var resp = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, CT);
		resp.StatusCode.Should().Be(HttpStatusCode.OK);

		await using var stream = await resp.Content.ReadAsStreamAsync(CT);
		using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: false);
		var lines = new List<string>();
		while (true)
		{
			var line = await reader.ReadLineAsync(CT).ConfigureAwait(false);
			if (line is null) break; // EOF
			if (line.Length == 0) continue; // skip blank
			lines.Add(line);
		}

		lines.Should().ContainInOrder("line-0", "line-1", "line-2", "line-3", "line-4");
		lines.Should().HaveCount(5);
	}

	private sealed class InfiniteStreamingHandler : IHttpHandler
	{
		private readonly TaskCompletionSource<bool> _tcs;
		public InfiniteStreamingHandler(TaskCompletionSource<bool> tcs) => _tcs = tcs;

		public Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
		{
			if (!string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase)
				|| !string.Equals(relativePath, "/infinite", StringComparison.Ordinal))
			{
				return Task.CompletedTask;
			}

			request.SetStreamingResponse("text/plain", async (writer, wct) =>
			{
				try
				{
					var i = 0;
					while (true)
					{
						await writer.WriteLineAsync("tick-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture)).ConfigureAwait(false);
						await writer.FlushAsync(wct).ConfigureAwait(false);
						await Task.Delay(10, wct).ConfigureAwait(false);
						i++;
					}
				}
				catch (OperationCanceledException)
				{
					// Cancellation propagated by server or delay
				}
				catch (IOException)
				{
					// Expected when client disconnects
				}
				catch (ObjectDisposedException)
				{
					// Also fine on disconnect
				}
				finally
				{
					_tcs.TrySetResult(true);
				}
			}, headers: null);

			return Task.CompletedTask;
		}
	}

	[TestMethod]
	public async Task Streaming_HandlerStops_OnClientDisconnect()
	{
		using var sut = new Server();
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var route = new RelativePathHandler("stream");
		route.RegisterHandler(new InfiniteStreamingHandler(tcs));
		sut.RegisterHandler(route);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "stream/infinite");

		using var client = new HttpClient();
		using (var resp = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, CT))
		{
			resp.StatusCode.Should().Be(HttpStatusCode.OK);
			await using var stream = await resp.Content.ReadAsStreamAsync(CT);
			using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: false);
			// Read a single line, then drop connection
			_ = await reader.ReadLineAsync(CT).ConfigureAwait(false);
		}

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5), CT));
		completed.Should().Be(tcs.Task, "handler should observe disconnect and stop promptly");
		(await tcs.Task).Should().BeTrue();
	}
}
