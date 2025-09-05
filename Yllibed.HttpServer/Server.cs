using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using Yllibed.Framework.Logging;
using Yllibed.HttpServer.Handlers;

#pragma warning disable MA0040 // Don't force using ct (netstd2.0 limitations)

namespace Yllibed.HttpServer;

/// <summary>
/// This will initialize a HttpServer in the process.
/// </summary>
/// <remarks>
/// Some capabilities could be required to accept connections from other machines on local network.
/// </remarks>
public sealed partial class Server : IHttpServer, IDisposable
{
	private const uint BufferSize = 8192;
	private readonly ushort _localPort;
	private readonly ServerOptions _options;

	private readonly CancellationTokenSource _cts = new CancellationTokenSource();

	private ImmutableList<IHttpHandler> _handlers = ImmutableList<IHttpHandler>.Empty;

	private (Uri Uri4, Uri Uri6)? _rootUris;
	private ImmutableArray<HttpServerRequest> _requests = ImmutableArray<HttpServerRequest>.Empty;

	// ReSharper disable InconsistentNaming
	private static readonly EventId CONNECTION_LOOP_ERROR = new EventId(0x01, nameof(CONNECTION_LOOP_ERROR));
	private static readonly EventId EVENT_HANDLER_ERROR = new EventId(0x02, nameof(EVENT_HANDLER_ERROR));
	// ReSharper restore InconsistentNaming

	private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

	/// <summary>
	/// Create a new server.
	/// </summary>
	/// <remarks>
	/// Not specifying a port will use a dynamic port assigned by the system.
	/// </remarks>
	/// <param name="localPort">Port number to listen to.  0=dynamic.</param>
	public Server(ushort localPort)
	{
		_localPort = localPort;
		_options = new ServerOptions { Port = localPort };
	}

	/// <summary>
	/// Create a new server with default settings (dynamic port).
	/// </summary>
	public Server() : this(localPort: 0)
	{
	}

	/// <summary>
	/// Create a new server with explicit options.
	/// </summary>
	/// <param name="options">Server options.</param>
	public Server(ServerOptions options)
	{
		_options = options ?? new ServerOptions();
		_localPort = _options.Port;
	}

	/// <summary>
	/// Create a new server with options resolved via Microsoft.Extensions.Options.
	/// </summary>
	/// <param name="options">A wrapper around <see cref="ServerOptions"/>.</param>
	[Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
	public Server(Microsoft.Extensions.Options.IOptions<ServerOptions> options)
		: this(options?.Value ?? new ServerOptions())
	{
	}

	private async Task HandleRequest(CancellationToken ct, HttpServerRequest request)
	{
		foreach (var handler in _handlers)
		{
			try
			{
				await handler.HandleRequest(ct, request, request.Path).ConfigureAwait(true);
			}
			catch (Exception ex)
			{
				this.Log().LogError(EVENT_HANDLER_ERROR, ex, "Error in event handler {Handler}", handler.ToString());
			}

			if (request.IsResponseSet)
			{
				break;
			}
		}

		if (!request.IsResponseSet)
		{
			request.SetResponse("text/plain", GetErrorContent(request), 404, "NOT FOUND");
		}
	}

	private string GetErrorContent(HttpServerRequest request) => $"Requested address {request.Url} not found.";

	private (Uri Uri4, Uri Uri6) Initialize()
	{
		var bind4 = _options?.BindAddress4 ?? IPAddress.Any;
		var bind6 = _options?.BindAddress6 ?? IPAddress.IPv6Any;

		var tcpListener4 = new TcpListener(bind4, _localPort);
		tcpListener4.Start();

		// Tentatively use the same port for IPv6
		var localPort4 = (ushort)((IPEndPoint)tcpListener4.LocalEndpoint).Port;
		var localPort6 = _localPort == 0
			? localPort4
			: _localPort;

		var tcpListener6 = new TcpListener(bind6, localPort6);
		tcpListener6.Start();

		var hostname4 = _options?.Hostname4 ?? "127.0.0.1";
		var hostname6 = _options?.Hostname6 ?? "::1";

		var uri4 = new Uri($"http://{hostname4}:{localPort4}");
		var uri6 = new Uri($"http://[{hostname6}]:{localPort6}");

		_ = Task.Run(() => HandleIncomingRequests(tcpListener4, _cts.Token), _cts.Token);
		_ = Task.Run(() => HandleIncomingRequests(tcpListener6, _cts.Token), _cts.Token);

		this.Log().LogInformation("Web server available on address v4 {Uri4}", uri4);
		this.Log().LogInformation("Web server available on address v6 {Uri6}", uri6);

		return (uri4, uri6);
	}

	private async Task HandleIncomingRequests(TcpListener listener, CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			var tcpClient = await listener.AcceptTcpClientAsync().ConfigureAwait(true);

			if (tcpClient is null || !tcpClient.Connected)
			{
				continue;
			}

			HttpServerRequest request = null!;
			request = new HttpServerRequest(
				tcpClient,
				_localPort,
				onReady: (r, ct2) => HandleRequest(ct2, r),
				onCompletedOrDisconnected: OnCompletedOrDisconnected,
				ct);

			void OnCompletedOrDisconnected() => ImmutableInterlocked.Update(ref _requests, (list, r2) => list.Remove(r2), request);

			ImmutableInterlocked.Update(ref _requests, (list, r) => list.Add(r), request);
		}
	}

	public void Dispose() => _cts.Cancel();

	public (Uri Uri4, Uri Uri6) Start()
	{
		if (_rootUris is { } uris)
		{
			return uris;
		}

		var rootUris = Initialize();
		_rootUris = rootUris;
		return rootUris;
	}

	public IDisposable RegisterHandler(IHttpHandler handler)
	{
		ImmutableInterlocked.Update(ref _handlers, (list, h) => list.Add(h), handler);

		return Disposable.Create(handler, h =>
		{
			ImmutableInterlocked.Update(ref _handlers, (list, h2) => list.Remove(h2), h);
			(h as IDisposable)?.Dispose();
		});

	}
}
