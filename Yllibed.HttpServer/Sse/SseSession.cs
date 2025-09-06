using System.IO;

namespace Yllibed.HttpServer.Sse;

internal sealed class SseSession : ISseSession
{
	private readonly TextWriter _writer;
	private readonly bool _autoFlush;
	private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
	private readonly CancellationTokenSource _sessionCts;
	private readonly string? _lastEventId;
	private CancellationToken SessionToken => _sessionCts.Token;
	public IHttpServerRequest Request { get; }
	public bool IsConnected => !_sessionCts.IsCancellationRequested;
	public string? LastEventId => _lastEventId;

	public SseSession(IHttpServerRequest request, TextWriter writer, CancellationTokenSource sessionCts, bool autoFlush)
	{
		Request = request ?? throw new ArgumentNullException(nameof(request));
		_writer = writer ?? throw new ArgumentNullException(nameof(writer));
		_sessionCts = sessionCts ?? throw new ArgumentNullException(nameof(sessionCts));
		_autoFlush = autoFlush;

		// Extract Last-Event-ID from request headers if present
		try
		{
			var headers = Request.Headers;
			if (headers != null && headers.TryGetValue("Last-Event-ID", out var values))
			{
				foreach (var v in values) { _lastEventId = v?.Trim(); break; }
			}
		}
		catch { /* ignore header access issues */ }
	}

	public async Task SendEventAsync(string data, string? eventName = null, string? id = null, CancellationToken ct = default)
	{
		await _mutex.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			try
			{
				await SseHelper.WriteEvent(_writer, data, eventName, id, ct).ConfigureAwait(false);
				if (_autoFlush)
				{
					await _writer.FlushAsync(ct).ConfigureAwait(false);
				}
			}
			catch (IOException)
			{
				_sessionCts.Cancel();
				throw new OperationCanceledException(ct.IsCancellationRequested ? ct : SessionToken);
			}
			catch (ObjectDisposedException)
			{
				_sessionCts.Cancel();
				throw new OperationCanceledException(ct.IsCancellationRequested ? ct : SessionToken);
			}
		}
		finally
		{
			_mutex.Release();
		}
	}

	public async Task SendCommentAsync(string comment, CancellationToken ct = default)
	{
		await _mutex.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			try
			{
				await SseHelper.WriteComment(_writer, comment, ct).ConfigureAwait(false);
				if (_autoFlush)
				{
					await _writer.FlushAsync(ct).ConfigureAwait(false);
				}
			}
			catch (IOException)
			{
				_sessionCts.Cancel();
				throw new OperationCanceledException(ct.IsCancellationRequested ? ct : SessionToken);
			}
			catch (ObjectDisposedException)
			{
				_sessionCts.Cancel();
				throw new OperationCanceledException(ct.IsCancellationRequested ? ct : SessionToken);
			}
		}
		finally
		{
			_mutex.Release();
		}
	}

	public async Task FlushAsync(CancellationToken ct = default)
	{
		await _mutex.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			try
			{
				await _writer.FlushAsync(ct).ConfigureAwait(false);
			}
			catch (IOException)
			{
				_sessionCts.Cancel();
				throw new OperationCanceledException(ct.IsCancellationRequested ? ct : SessionToken);
			}
			catch (ObjectDisposedException)
			{
				_sessionCts.Cancel();
				throw new OperationCanceledException(ct.IsCancellationRequested ? ct : SessionToken);
			}
		}
		finally
		{
			_mutex.Release();
		}
	}
}
