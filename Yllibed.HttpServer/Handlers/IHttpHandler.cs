using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.HttpServer.Handlers;

/// <summary>
/// This is the interface who should be implemented by a Http Server Handler.
/// </summary>
/// <remarks>
/// The handler should check if the request is "interesting" and produce a result.
/// If not interested, should simply leave the request untouched.
/// </remarks>
public interface IHttpHandler
{
	Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath);
}
