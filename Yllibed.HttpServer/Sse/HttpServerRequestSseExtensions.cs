using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.HttpServer.Sse;

public static class HttpServerRequestSseExtensions
{
	/// <summary>
	/// Starts a Server-Sent Events (SSE) session and passes an <see cref="ISseSession"/> to your handler lambda.
	/// </summary>
	/// <remarks>
	/// SSE framing per WHATWG HTML Living Standard.
	/// HTTP body delimitation: this server uses close-delimited messages (Connection: close) which is valid in HTTP/1.1
	/// as per RFC 7230 §3.3.3 (superseded by RFC 9112 §6.1). No chunked encoding is used.
	/// </remarks>
	public static void StartSseSession(
		this IHttpServerRequest request,
		Func<ISseSession, CancellationToken, Task> sessionHandler,
		uint resultCode = 200,
		string resultText = "OK",
		IReadOnlyDictionary<string, IReadOnlyCollection<string>>? headers = null,
		SseOptions? options = null)
	{
		if (request is null) throw new ArgumentNullException(nameof(request));
		if (sessionHandler is null) throw new ArgumentNullException(nameof(sessionHandler));

		var effectiveHeaders = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
		if (headers != null)
		{
			foreach (var kvp in headers)
			{
				effectiveHeaders[kvp.Key] = kvp.Value;
			}
		}
		if (!effectiveHeaders.ContainsKey("Cache-Control"))
		{
			effectiveHeaders["Cache-Control"] = new[] { "no-cache" };
		}

		// Always set text/event-stream for SSE
		var contentType = "text/event-stream";
		request.SetStreamingResponse(contentType, async (writer, ct) =>
		{
			// Per SSE best practices, use LF line endings; our response writer already writes CRLF for headers.
			writer.NewLine = "\n";
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			var session = new SseSession(request, writer, linkedCts, autoFlush: options?.AutoFlush ?? true);
			var heartbeatTask = Task.CompletedTask;
			var hbInterval = options?.HeartbeatInterval ?? TimeSpan.Zero;
			if (hbInterval > TimeSpan.Zero)
			{
				heartbeatTask = Task.Run(async () =>
				{
					try
					{
						while (!linkedCts.IsCancellationRequested)
						{
							await Task.Delay(hbInterval, linkedCts.Token).ConfigureAwait(false);
							if (linkedCts.IsCancellationRequested) break;
							await session.SendCommentAsync(options?.HeartbeatComment ?? "keepalive", linkedCts.Token).ConfigureAwait(false);
						}
					}
					catch (OperationCanceledException)
					{
						// normal on shutdown
					}
				}, linkedCts.Token);
			}

			try
			{
				await sessionHandler(session, linkedCts.Token).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				// client or server requested cancellation – close stream gracefully
			}
			catch (IOException)
			{
				// network failure – close stream gracefully
			}
			catch (ObjectDisposedException)
			{
				// connection disposed – close stream gracefully
			}
			finally
			{
				// Stop heartbeat and wait for it to complete
				linkedCts.Cancel();
				try { await heartbeatTask.ConfigureAwait(false); } catch { /* ignore */ }
			}
		}, resultCode, resultText, effectiveHeaders);
	}
}
