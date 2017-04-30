#define SERVER_LOGGING

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Yllibed.Framework.Concurrency;
using Yllibed.Framework.Extensions;
using Yllibed.Framework.Logging;
using Yllibed.HttpServer.Handlers;

#if NETFX_CORE
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif

namespace Yllibed.HttpServer
{
	/// <summary>
	/// This will initialize a HttpServer in the process.
	/// </summary>
	/// <remarks>
	/// Some capabilities could be required to accept connections from other machines on local network.
	/// </remarks>
	public class HttpServer : IHttpServer, IDisposable
	{
		private const uint BufferSize = 8192;
		private readonly IScheduler _scheduler;
		private ushort _localPort;

		private readonly CompositeDisposable _requests = new CompositeDisposable();

		private readonly SingleAssignmentDisposable _incomingRequests4 = new SingleAssignmentDisposable();
		private readonly SingleAssignmentDisposable _incomingRequests6 = new SingleAssignmentDisposable();

		private ImmutableList<IHttpHandler> _handlers = ImmutableList<IHttpHandler>.Empty;

		private TcpListener _tcpListener4;

		// ReSharper disable InconsistentNaming
		private static readonly EventId CONNECTION_LOOP_ERROR = new EventId(0x01, nameof(CONNECTION_LOOP_ERROR));
		private static readonly EventId EVENT_HANDLER_ERROR = new EventId(0x02, nameof(EVENT_HANDLER_ERROR));
		// ReSharper restore InconsistentNaming

		private static readonly Encoding Utf8 = new UTF8Encoding(false, false);

		/// <summary>
		/// Create a new server.
		/// </summary>
		/// <remarks>
		/// Not specifying a port will let the OS pick one available.
		/// </remarks>
		/// <param name="localPort">Port number to listen to.  0=dynamic.</param>
		/// <param name="scheduler">The scheduler to use for requests processing. (default to TaskPool)</param>
		public HttpServer(ushort localPort = 0, IScheduler scheduler = null)
		{
			_scheduler = scheduler ?? TaskPoolScheduler.Default;
			_localPort = localPort;
		}

		private async Task HandleRequest(CancellationToken ct, HttpServerRequest request)
		{
			foreach (var handler in _handlers)
			{
				try
				{
					await handler.HandleRequest(ct, request, request.Path);
				}
				catch (Exception ex)
				{
					this.Log().LogError(EVENT_HANDLER_ERROR, ex, "Error in event Handler.");
				}

				if (request.IsResponseSet)
				{
					break;
				}
			}

			if (!request.IsResponseSet)
			{
				request.SetResponse("text/plain", ct2 => GetErrorContent(request, ct2), 404, "NOT FOUND");
			}
		}

#pragma warning disable 1998
		private async Task<Stream> GetErrorContent(HttpServerRequest request, CancellationToken ct)
		{
			return new MemoryStream(Utf8.GetBytes($"Requested address {request.Url} not found."));
		}
#pragma warning restore 1998

		private Uri Initialize()
		{
			_tcpListener4 = new TcpListener(IPAddress.Any, _localPort);
			_tcpListener4.Start();

			// Tentatively use the same port for IPv6
			_localPort = (ushort)((IPEndPoint) _tcpListener4.LocalEndpoint).Port;

			_tcpListener6 = new TcpListener(IPAddress.IPv6Any, _localPort);
			_tcpListener6.Start();

#if NETFX_CORE
			var hostname4 =
				NetworkInformation
					.GetHostNames()
					.Where(x => x.Type == HostNameType.Ipv4)
					.Select(x => x.CanonicalName)
					.FirstOrDefault()
				?? "127.0.0.1";

			var hostname6 =
				NetworkInformation
					.GetHostNames()
					.Where(x => x.Type == HostNameType.Ipv6)
					.Select(x => x.CanonicalName)
					.FirstOrDefault()
				?? "::1";
#else
			const string hostname4 = "127.0.0.1";
			const string hostname6 = "::1";
#endif

			var uri4 = new Uri($"http://{hostname4}:{_localPort}");
			var uri6 = new Uri($"http://[{hostname6}]:{_localPort}");

			_incomingRequests4.Disposable = _scheduler.Schedule(HandleIncomingRequests4);
			_incomingRequests6.Disposable = _scheduler.Schedule(HandleIncomingRequests6);

#if SERVER_LOGGING
			this.Log().Info($"Web server available on address v4 {uri4}");
			this.Log().Info($"Web server available on address v6 {uri6}");
#endif
			return uri4;
		}

		private async Task HandleIncomingRequests4(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				var tcpClient = await _tcpListener4.AcceptTcpClientAsync();
				var request = new HttpServerRequest(_scheduler, tcpClient, _localPort);
				request.Init(() => _requests.Remove(request), ct2 => HandleRequest(ct2, request));
				_requests.Add(request);
			}
		}

		private async Task HandleIncomingRequests6(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				var tcpClient = await _tcpListener6.AcceptTcpClientAsync();
				var request = new HttpServerRequest(_scheduler, tcpClient, _localPort);
				request.Init(() => _requests.Remove(request), ct2 => HandleRequest(ct2, request));
				_requests.Add(request);
			}
		}

		public void Dispose()
		{
			_tcpListener4?.Stop();

			_incomingRequests4.Dispose();
			_incomingRequests6.Dispose();

			_requests.Dispose();
		}

		private Uri _rootUri;
		private TcpListener _tcpListener6;

#pragma warning disable 1998
		public async Task<Uri> GetRootUri(CancellationToken ct)
		{
			return _rootUri ?? (_rootUri = Initialize());
		}
#pragma warning restore 1998

		public IDisposable RegisterHandler(IHttpHandler handler)
		{
			Transactional.Update(ref _handlers, handler, (list, h) => list.Add(h));
			_handlers.Add(handler);

			return Disposable.Create(() =>
			{
				Transactional.Update(ref _handlers, handler, (list, h) => list.Remove(h));
				(handler as IDisposable)?.Dispose();
			});
		}

		private class HttpServerRequest : IDisposable, IHttpServerRequest
		{
			private readonly IScheduler _scheduler;
			private readonly TcpClient _tcpClient;
			private Action _onDisconnect;
			private Func<CancellationToken, Task> _onReady;
			private IDisposable _subscription;

			internal HttpServerRequest(
				IScheduler scheduler,
				TcpClient tcpClient,
				int port)
			{
				_scheduler = scheduler;
				_tcpClient = tcpClient;

				Port = port;
			}

			internal void Init(Action onDisconnect, Func<CancellationToken, Task> onReady)
			{
				_onDisconnect = onDisconnect;
				_onReady = onReady;
				_subscription = _scheduler.Schedule(ProcessConnection);
			}

			private async Task ProcessConnection(CancellationToken ct)
			{
				await Task.Yield(); // deffer starting of the whole process

				if (ct.IsCancellationRequested)
				{
					return;
				}

				try
				{
					using (var stream = _tcpClient.GetStream())
					{
						await ProcessRequest(ct, stream);

#if SERVER_LOGGING
						this.Log().Info($"Received request on url {Url}");
#endif
						await _onReady(ct);

#if SERVER_LOGGING
						this.Log().Info($"Response for url {Url}: {_responseResultCode} {_responseResultText}");
#endif

						await ProcessResponse(ct, stream);
					}
				}
				catch (Exception ex)
				{
					this.Log().LogError(CONNECTION_LOOP_ERROR, ex, "Error processing request " + Path);
				}
				finally
				{
					_onDisconnect();
				}
			}

			// Parsing the charset using RFC2046 4.5.1 https://tools.ietf.org/html/rfc2046#section-4.5.1
			private static Regex ContentTypeCharsetRegex = new Regex(@"\Wcharset=(?<charset>[\w\-]+)");

			private async Task ProcessRequest(CancellationToken ct, Stream stream)
			{
				var encoding = GetRequestEncoding();

				using (var requestReader = new StreamReader(stream, encoding, true, (int) BufferSize, true))
				{
					var requestLine = await requestReader.ReadLineAsync();
					var requestLineParts = requestLine.Split(' ');
					if (requestLineParts.Length < 2)
					{
						throw new Exception("Invalid Request Line");
					}

					Method = requestLineParts[0];
					Path = requestLineParts[1]; // "absolute-form" as RFC 7230 section 5.3.2 not supported in this implementation. https://tools.ietf.org/html/rfc7230#section-5.3.2
					if (requestLineParts.Length > 2)
					{
						Http = requestLineParts[2];
					}

					var requestHeaders = new Dictionary<string, ImmutableList<string>>();

					var headerLine = await requestReader.ReadLineAsync();
					while (!(ct.IsCancellationRequested || headerLine.IsNullOrWhiteSpace()))
					{
						(var header, var value) = ParseHeader(headerLine);

						if (requestHeaders.ContainsKey(header))
						{
							requestHeaders[header] = requestHeaders[header].Add(value);
						}
						else
						{
							requestHeaders[header] = ImmutableList.Create(value);
						}

						headerLine = await requestReader.ReadLineAsync();
					}

					Headers = requestHeaders.ToImmutableDictionary();

					if (ContentLength.HasValue)
					{
						var chars = new char[ContentLength.Value];
						if (chars.Length > 0)
						{
							var length = await requestReader.ReadBlockAsync(chars, 0, ContentLength.Value);
							Body = new string(chars, 0, length);
						}
						else
						{
							Body = string.Empty;
						}
					}
				}
			}

			private Encoding GetRequestEncoding()
			{
				if (!ContentType.IsNullOrWhiteSpace())
				{
					var match = ContentTypeCharsetRegex.Match(ContentType);
					if (match.Success)
					{
						try
						{
							var charset = match.Groups["charset"].Value;
							return Encoding.GetEncoding(charset);
						}
						catch
						{
							// On exception in "GetEncoding", fallback to default value.
						}
					}
				}
				return Utf8; // default value if an error or not specified
			}

			private async Task ProcessResponse(CancellationToken ct, NetworkStream stream)
			{
				using (var responseWriter = new StreamWriter(stream, Utf8, (int) BufferSize, true) {NewLine = "\r\n"})
				{
					await ProcessResponseHeader(responseWriter);

					await responseWriter.FlushAsync();

					await ProcessResponsePayload(ct, responseWriter, stream);
				}
			}

			private async Task ProcessResponseHeader(TextWriter responseWriter)
			{
				// Response Line
				await responseWriter.WriteFormattedLineAsync($"HTTP/1.1 {_responseResultCode} {_responseResultText}");
				await responseWriter.FlushAsync();

				// Content-Type
				await responseWriter.WriteFormattedLineAsync($"Content-Type: {_responseContentType}");

				// HTTP 1.1 mode (keep-alive not supported yet)
				await responseWriter.WriteLineAsync("Connection: close");

				if (_responseHeaders != null)
				{
					foreach (var header in _responseHeaders)
					{
						var key = header.Key.Trim();

						switch (key)
						{
							case null:
							case "":
							case "Content-Type":
							case "Connection":
								continue;
						}

						foreach (var value in header.Value)
						{
							await responseWriter.WriteFormattedLineAsync($"{header.Key}: {value}");
						}
					}
				}
			}

			private async Task ProcessResponsePayload(CancellationToken ct, TextWriter responseWriter, Stream responseStream)
			{
				using (var streamToSend = await _responseStreamFactory(ct))
				{
					// Content-Length header
					await responseWriter.WriteFormattedLineAsync($"Content-Length: {streamToSend.Length}");
					await responseWriter.WriteLineAsync();

					await responseWriter.FlushAsync();

					await streamToSend.CopyToAsync(responseStream);
				}
			}

			private static readonly char[] HeaderSeparator = {':'};

			private (string header, string content) ParseHeader(string line)
			{
				var parts = line.Split(HeaderSeparator, 2);
				if (parts.Length != 2)
				{
					return (null, null);
				}

				switch (parts[0].ToUpperInvariant())
				{
					case "HOST":
						Host = parts[1].Trim();
						HostName = Host.Split(':')[0].Trim();
						break;
					case "REFERER":
					case "REFERRER":
						Referer = parts[1].Trim();
						break;
					case "USER-AGENT":
						UserAgent = parts[1].Trim();
						break;
					case "ACCEPT":
						Accept = parts[1].Trim();
						break;
					case "CONTENT-TYPE":
						ContentType = parts[1].Trim();
						break;
					case "CONTENT-LENGTH":
						int i;
						if (int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
						{
							ContentLength = i;
						}

						break;
				}

				return (parts[0], parts[1]);
			}

			public string Method { get; private set; }

			public string Path { get; private set; }

			public string Http { get; private set; }

			public string Host { get; private set; }

			public string HostName { get; private set; }

			public int Port { get; }

			public string Referer { get; private set; }

			public string UserAgent { get; private set; }

			public string ContentType { get; private set; }

			public int? ContentLength { get; private set; }

			public string Body { get; private set; }

			public string Accept { get; private set; }

			public Uri Url => _url ?? (_url = new Uri("http://" + Host + Path, UriKind.Absolute));

			public ImmutableDictionary<string, ImmutableList<string>> Headers { get; private set; }

			public void SetResponse(
				string contentType,
				Func<CancellationToken, Task<Stream>> streamFactory,
				uint resultCode,
				string resultText,
				IDictionary<string, ImmutableList<string>> headers = null)
			{
				_responseContentType = contentType;
				_responseResultCode = resultCode;
				_responseResultText = resultText;
				_responseStreamFactory = streamFactory;
				_responseHeaders = headers;

				IsResponseSet = true;
			}

			private string _responseContentType;
			private uint _responseResultCode;
			private string _responseResultText;
			private Func<CancellationToken, Task<Stream>> _responseStreamFactory;
			private IDictionary<string, ImmutableList<string>> _responseHeaders;
			private Uri _url;

			internal bool IsResponseSet { get; private set; }

			public void Dispose()
			{
				_tcpClient.Dispose();
				_subscription.Dispose();
			}
		}
	}
}
