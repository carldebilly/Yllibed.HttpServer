using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.HttpServer.Handlers
{
	/// <summary>
	/// This is a simple notify handler
	/// </summary>
	/// <remarks>
	/// Each time a POST is sent to the notification url (/notify is the default value), the
	/// Notifications observable will produce the request body.
	/// This is used to hook an internal process to an external POST event.
	/// </remarks>
	public class NotifyHandler : INotifyHandler, IDisposable
	{
		private readonly string _notifyPath;

		public NotifyHandler(string notifyPath = "/notify")
		{
			_notifyPath = notifyPath;
		}

		private readonly Subject<string> _notifications = new Subject<string>();

#pragma warning disable 1998
		public async Task HandleRequest(CancellationToken ct, Uri serverRoot, string relativePath, IHttpServerRequest request)
		{
			if (relativePath.Equals(_notifyPath, StringComparison.OrdinalIgnoreCase))
			{
				if (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					request.SetResponse("text/plain", Create200);
					_notifications.OnNext(request.Body);
				}
				else
				{
					request.SetResponse("text/plain", Create405, 405, "METHOD NOT ALLOWED");
				}
			}
		}

		private static Task<Stream> Create200(CancellationToken ct)
		{
			var stream = new MemoryStream(Encoding.UTF8.GetBytes("Notified"));
			return Task.FromResult(stream as Stream);
		}

		private static Task<Stream> Create405(CancellationToken ct)
		{
			var stream = new MemoryStream(Encoding.UTF8.GetBytes("Method not authorized - use a POST"));
			return Task.FromResult(stream as Stream);
		}
#pragma warning restore 1998

		public IObservable<string> Notifications => _notifications;

		public void Dispose()
		{
			_notifications.Dispose();
		}
	}
}
