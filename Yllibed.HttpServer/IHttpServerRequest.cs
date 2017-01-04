using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.HttpServer
{
	public interface IHttpServerRequest
	{
		/// <summary>
		/// The HTTP METHOD used.  Usually GET, POST, PUT, DELETE...
		/// </summary>
		string Method { get; }

		/// <summary>
		/// The full path & querystring of the request.
		/// </summary>
		/// <remarks>
		/// This is the exact string sent by the user-agent (browser) right after the Http Method.  It means it's url-encoded.
		/// See RFC 7230 section 5.3 for more details. https://tools.ietf.org/html/rfc7230#section-5.3
		/// </remarks>
		string Path { get; }

		/// <summary>
		/// This is the Http Protocol used.
		/// </summary>
		/// <remarks>
		/// Should usually be "HTTP/1.1"
		/// </remarks>
		string Http { get; }

		/// <summary>
		/// This is the "Host" request header: the hostname resolved by user-agent to reach the server.
		/// </summary>
		/// <remarks>
		/// This is the exact "host" header sent by the user-agent (browser).
		/// See RFC 7230 section 5.3 for more details. https://tools.ietf.org/html/rfc7230#section-5.3
		/// </remarks>
		string Host { get; }

		/// <summary>
		/// Only the "hostname" part of the "Host" header.
		/// </summary>
		/// <remarks>
		/// This is a parsed version of the "Host" header.
		/// </remarks>
		string HostName { get; }

		/// <summary>
		/// This is the local port on which the connection was received. It's the port of the server.
		/// </summary>
		int Port { get; }

		/// <summary>
		/// This is the unparsed "referer" string sent by the user-agent.
		/// </summary>
		/// <remarks>
		/// See RFC 7231 section 5.5.2 for more details. https://tools.ietf.org/html/rfc7231#section-5.5.2
		/// </remarks>
		string Referer { get; }

		/// <summary>
		/// This is the unparsed user-agent string sent by the user-agent.
		/// </summary>
		/// <remarks>
		/// The interest of this header is limited. Avoid using it to decide what to reply.
		/// See RFC 7231 section 5.5.3 for more details. https://tools.ietf.org/html/rfc7231#section-5.5.3
		/// </remarks>
		string UserAgent { get; }

		/// <summary>
		/// This is the completed url used by the user agent to make the request.
		/// </summary>
		Uri Url { get; }

		/// <summary>
		/// Length of the request body (for http methods supporting it)
		/// </summary>
		/// <remarks>
		/// null means no request body.
		/// </remarks>
		int? ContentLength { get; }

		/// <summary>
		/// Content-type of the request body
		/// </summary>
		/// <remarks>
		/// This field is unparsed. Could contains information about the charset used in the request payload.
		/// See RFC 7231 section 3.1.1.5 for more details. https://tools.ietf.org/html/rfc7231#section-3.1.1.5
		/// </remarks>
		string ContentType { get; }

		/// <summary>
		/// This is the body of the request, if any.
		/// </summary>
		/// <remarks>
		/// The body has been converted to string using specified encoding charset, if any.
		/// Binary content not supported in this version.
		/// </remarks>
		string Body { get; }

		/// <summary>
		/// This is the unparsed "Accept" header from user-agent request.
		/// </summary>
		/// <remarks>
		/// Could contain the port number.
		/// See RFC 7231 section 5.3.2 for more details. https://tools.ietf.org/html/rfc7231#section-5.3.2
		/// </remarks>
		string Accept { get; }

		ImmutableDictionary<string, ImmutableList<string>> Headers { get; }

		/// <summary>
		/// This method is called by handler to set the result to a request.
		/// </summary>
		/// <param name="streamFactory">A delegate to create a stream.  THIS STREAM WILL BE DISPOSED AUTOMATICALLY WHEN SENT TO CLIENT USER-AGENT.</param>
		void SetResponse(
			string contentType,
			Func<CancellationToken, Task<Stream>> streamFactory,
			uint resultCode = 200,
			string resultText = "OK",
			IDictionary<string, ImmutableList<string>> headers = null);
	}
}