using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yllibed.HttpServer.Extensions;

namespace Yllibed.HttpServer.Handlers;

public class RelativePathHandler : IHttpHandler, IDisposable
{
	private readonly string _path;

	public RelativePathHandler(string path)
	{
		if (path is null or { Length: 0 })
		{
			throw new ArgumentNullException(nameof(path));
		}

		if (!path.StartsWith("/", StringComparison.Ordinal))
		{
			path = "/" + path;
		}

		_path = path;
	}

	private ImmutableList<IHttpHandler> _handlers = ImmutableList<IHttpHandler>.Empty;

	/// <summary>
	/// Create  a handler for this relative path
	/// </summary>
	/// <remarks>
	/// Disposing the return value will remove the unregister the handler.
	/// </remarks>
	public IDisposable RegisterHandler(IHttpHandler handler)
	{
		if (_handlers == null)
		{
			throw new ObjectDisposedException(nameof(RelativePathHandler));
		}

		ImmutableInterlocked.Update(ref _handlers, (list, h) => list.Add(h), handler);

		return Disposable.Create(handler, h =>
		{
			ImmutableInterlocked.Update(ref _handlers, (list, h2) => list.Remove(h2), h);
			(h as IDisposable)?.Dispose();
		});
	}

	async Task IHttpHandler.HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
	{
		var handlers = _handlers;
		if (!handlers.Any())
		{
			return; // nothing to do
		}

		if (relativePath.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
		{
			var subPath = relativePath.Substring(_path.Length);
			if (!subPath.StartsWith("/", StringComparison.Ordinal))
			{
				subPath = "/" + subPath;
			}

			foreach (var handler in handlers)
			{
				await handler.HandleRequest(ct, request, subPath).ConfigureAwait(true);
			}
		}
	}

	public void Dispose()
	{
		var handlers = Interlocked.Exchange(ref _handlers, ImmutableList<IHttpHandler>.Empty);

		foreach (var handler in handlers)
		{
			(handler as IDisposable)?.Dispose();
		}
	}
}
