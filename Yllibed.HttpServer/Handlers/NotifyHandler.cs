using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1998

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

		public async Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
		{
			if (relativePath.Equals(_notifyPath, StringComparison.OrdinalIgnoreCase))
			{
				if (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					request.SetResponse("text/plain", "Notified");
					_notifications.OnNext(request.Body);
				}
				else
				{
					request.SetResponse("text/plain", "Method not authorized - use a POST", 405, "METHOD NOT ALLOWED");
				}
			}
		}

		public IObservable<string> Notifications => _notifications;

		public void Dispose()
		{
			_notifications.Dispose();
		}
	}
}
