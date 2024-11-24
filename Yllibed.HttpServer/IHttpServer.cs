using System;
using System.Threading;
using System.Threading.Tasks;
using Yllibed.HttpServer.Handlers;

namespace Yllibed.HttpServer;

/// <summary>
/// Represent a http versatile http server
/// </summary>
public interface IHttpServer
{
	/// <summary>
	/// Get the root uri for the server on localhost.
	/// </summary>
	/// <remarks>
	/// To connect from another host, you should reuse the port number with any IP address of the device.
	/// </remarks>
	/// <returns>Uri to localhost with port number for http protocol. The port will usually listen on other IPs too.</returns>
	(Uri Uri4, Uri Uri6) Start();

	/// <summary>
	/// Register an handler for http requests.
	/// </summary>
	/// <remarks>
	/// All requests will be passed through all handlers, in order they was registered, until a response is produced.
	/// The first handler to produce a result wins, others are not called.
	/// If no handler set a result, a 404 will be sent.
	/// </remarks>
	/// <returns>A registration. Disposing it will unregister the handler and dispose it (if implementing IDisposable).</returns>
	IDisposable RegisterHandler(IHttpHandler handler);
}
