using System;
using Yllibed.HttpServer.Handlers;

namespace Yllibed.HttpServer
{
	public interface INotifyHandler : IHttpHandler
	{
		IObservable<string> Notifications { get; }
	}
}