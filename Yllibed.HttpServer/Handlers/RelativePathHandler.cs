using System;
using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Yllibed.Framework.Concurrency;
using Yllibed.Framework.Extensions;

namespace Yllibed.HttpServer.Handlers
{
	public class RelativePathHandler : IHttpHandler, IDisposable
	{
		private readonly string _path;

		public RelativePathHandler(string path)
		{
			if (path.IsNullOrWhiteSpace())
			{
				throw new ArgumentNullException(nameof(path));
			}

			if (!path.StartsWith("/"))
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

			Transactional.Update(ref _handlers, handler, (list, h) => list.Add(h));
			_handlers.Add(handler);

			return Disposable.Create(() =>
			{
				Transactional.Update(ref _handlers, handler, (list, h) => list.Remove(h));
				(handler as IDisposable)?.Dispose();
			});
		}

		async Task IHttpHandler.HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
		{
			var handlers = _handlers;
			if (handlers.IsNullOrEmpty())
			{
				return; // nothing to do
			}

			if (relativePath.StartsWith(_path, StringComparison.OrdinalIgnoreCase))
			{
				var subPath = relativePath.Substring(_path.Length);
				if (!subPath.StartsWith("/"))
				{
					subPath = "/" + subPath;
				}

				foreach (var handler in handlers)
				{
					await handler.HandleRequest(ct, request, subPath);
				}
			}
		}

		public void Dispose()
		{
			var handlers = Interlocked.Exchange(ref _handlers, null);
			if (handlers == null)
			{
				return; // already disposed
			}

			foreach (var handler in handlers)
			{
				(handler as IDisposable)?.Dispose();
			}
		}
	}
}