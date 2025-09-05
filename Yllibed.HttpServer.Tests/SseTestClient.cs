#pragma warning disable MA0001 // IndexOf StringComparison analyzer not applicable for char overloads here
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Yllibed.HttpServer.Tests;

internal static class SseTestClient
{
	internal sealed record ServerSentEvent(string? Event, string? Id, string Data);

	internal sealed class SseConnection : IAsyncDisposable, IDisposable
	{
		private readonly HttpClient _client;
		private readonly HttpResponseMessage _response;
		private Stream? _stream;

		public SseConnection(HttpClient client, HttpResponseMessage response)
		{
			_client = client;
			_response = response;
		}

		public HttpResponseMessage Response => _response;

		public async IAsyncEnumerable<ServerSentEvent> ReadEventsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			_stream ??= await _response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			await foreach (var ev in ReadFromStreamAsync(_stream, cancellationToken))
			{
				yield return ev;
			}
		}

		public void Dispose()
		{
			_stream?.Dispose();
			_response.Dispose();
			_client.Dispose();
		}

		public async ValueTask DisposeAsync()
		{
			if (_stream is IAsyncDisposable ad)
			{
				await ad.DisposeAsync().ConfigureAwait(false);
			}
			else
			{
				_stream?.Dispose();
			}
			_response.Dispose();
			_client.Dispose();
		}
	}

	public static async Task<SseConnection> ConnectAsync(Uri uri, CancellationToken ct, string? accept = null, string? lastEventId = null)
	{
		var client = new HttpClient();
		var req = new HttpRequestMessage(HttpMethod.Get, uri);
		if (!string.IsNullOrEmpty(accept))
		{
			req.Headers.Accept.Clear();
			req.Headers.Accept.ParseAdd(accept);
		}
		if (!string.IsNullOrEmpty(lastEventId))
		{
			req.Headers.TryAddWithoutValidation("Last-Event-ID", lastEventId);
		}
		var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
		return new SseConnection(client, resp);
	}

	public static async IAsyncEnumerable<ServerSentEvent> ReadFromStreamAsync(Stream stream,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: false);

		string? ev = null;
		string? id = null;
		var dataBuilder = new StringBuilder();

		while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
		{
			var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) ?? string.Empty;

			if (line.Length == 0)
			{
				if (dataBuilder.Length > 0)
				{
					var data = dataBuilder.ToString().TrimEnd('\n');
					yield return new ServerSentEvent(ev, id, data);
					dataBuilder.Clear();
					ev = null; // id is sticky per spec; do not clear id here
				}
				continue;
			}

			if (line[0] == ':')
			{
				// comment: ignore for now
				continue;
			}

			var idx = -1;
			for (var i = 0; i < line.Length; i++)
			{
				if (line[i] == ':')
				{
					idx = i; break;
				}
			}
			string field, value;
			if (idx == -1)
			{
				field = line;
				value = string.Empty;
			}
			else
			{
				field = line[..idx];
				value = (idx + 1 < line.Length && line[idx + 1] == ' ')
					? line[(idx + 2)..]
					: line[(idx + 1)..];
			}

			switch (field)
			{
				case "event":
					ev = value;
					break;
				case "data":
					dataBuilder.Append(value).Append('\n');
					break;
				case "id":
					id = value; // sticky across events
					break;
				case "retry":
					// ignore in tests
					break;
			}
		}
	}
}
