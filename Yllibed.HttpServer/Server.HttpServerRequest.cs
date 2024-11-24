using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Yllibed.Framework.Logging;
using Yllibed.HttpServer.Extensions;

#pragma warning disable MA0040 // Don't force using ct (netstd2.0 limitations)

namespace Yllibed.HttpServer;

public sealed partial class Server
{
	[DebuggerDisplay("Request {Method} {Path} #{_requestId}")]
	private sealed class HttpServerRequest : IDisposable, IHttpServerRequest
	{
		private readonly TcpClient _tcpClient;
		private readonly CancellationToken _ct;
		private Func<HttpServerRequest, CancellationToken, Task> _onReady;
		private readonly Action _onCompletedOrDisconnected;

		private static long NextRequestId = 0;
		private readonly long _requestId = Interlocked.Increment(ref NextRequestId);

		internal HttpServerRequest(
			TcpClient tcpClient,
			int port,
			Func<HttpServerRequest, CancellationToken, Task> onReady,
			Action onCompletedOrDisconnected,
			CancellationToken ct)
		{
			_tcpClient = tcpClient;
			_onReady = onReady;
			_onCompletedOrDisconnected = onCompletedOrDisconnected;
			_ct = ct;

			Port = port;

			_ = ProcessConnection(_ct);
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
					await ProcessRequest(ct, stream).ConfigureAwait(true);

					this.Log().LogInformation("Received request on url {Url}", Url);
					await _onReady(this, ct).ConfigureAwait(true);

					this.Log().LogInformation("Response for url {Url}: {Code} {ResultText}", Url, _responseResultCode, _responseResultText);

					await ProcessResponse(ct, stream).ConfigureAwait(true);
				}
			}
			catch (Exception ex)
			{
				this.Log().LogError(CONNECTION_LOOP_ERROR, ex, "Error processing request {Path} ", Path);
			}
			finally
			{
				_onCompletedOrDisconnected();
			}
		}

		// Parsing the charset using RFC2046 4.5.1 https://tools.ietf.org/html/rfc2046#section-4.5.1
		private static readonly Regex ContentTypeCharsetRegex = new Regex(
			@"\Wcharset=(?<charset>[\w\-]+)",
			RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline,
			TimeSpan.FromMilliseconds(250));

		private async Task ProcessRequest(CancellationToken ct, Stream stream)
		{
			var encoding = GetRequestEncoding();

			using var requestReader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, (int) BufferSize, leaveOpen: true);
			var requestLine = await requestReader.ReadLineAsync().ConfigureAwait(true);
			var requestLineParts = requestLine?.Split(' ');
			if (requestLineParts is not {Length: 3})
			{
				throw new InvalidOperationException("Invalid request line");
			}

			Method = requestLineParts[0];
			Path = requestLineParts[1]; // "absolute-form" as RFC 7230 section 5.3.2 not supported in this implementation. https://tools.ietf.org/html/rfc7230#section-5.3.2
			if (requestLineParts.Length > 2)
			{
				Http = requestLineParts[2];
			}

			var requestHeaders = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);

			var headerLine = await requestReader.ReadLineAsync().ConfigureAwait(true);
			while (headerLine is { Length: > 0 } && !ct.IsCancellationRequested)
			{
				var (header, value) = ParseHeader(headerLine);

				if (header is not null && value is not null)
				{
					if (requestHeaders.TryGetValue(header, out var requestHeader))
					{
						((List<string>)requestHeader).Add(value);
					}
					else
					{
						requestHeaders[header] = new List<string> { value };
					}
				}

				headerLine = await requestReader.ReadLineAsync().ConfigureAwait(true);
			}

			Headers = requestHeaders;

			if (ContentLength.HasValue)
			{
				var chars = new char[ContentLength.Value];
				if (chars.Length > 0)
				{
					var length = await requestReader.ReadBlockAsync(chars, 0, ContentLength.Value).ConfigureAwait(true);
					Body = new string(chars, 0, length);
				}
				else
				{
					Body = string.Empty;
				}
			}
		}

		private Encoding GetRequestEncoding()
		{
			if (ContentType is { Length: > 0 }
			    && ContentTypeCharsetRegex.Match(ContentType) is { Success: true } match)
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
			return Utf8; // default value if an error or not specified
		}

		private async Task ProcessResponse(CancellationToken ct, NetworkStream stream)
		{
			using var responseWriter = new StreamWriter(stream, Utf8, (int) BufferSize, leaveOpen: true);
			responseWriter.NewLine = "\r\n";

			await ProcessResponseHeader(responseWriter).ConfigureAwait(true);

			await responseWriter.FlushAsync().ConfigureAwait(true);

			await ProcessResponsePayload(ct, responseWriter, stream).ConfigureAwait(true);
		}

		private async Task ProcessResponseHeader(TextWriter responseWriter)
		{
			// Response Line
			await responseWriter.WriteFormattedLineAsync($"HTTP/1.1 {_responseResultCode} {_responseResultText}").ConfigureAwait(true);
			await responseWriter.FlushAsync().ConfigureAwait(true);

			// Content-Type
			await responseWriter.WriteFormattedLineAsync($"Content-Type: {_responseContentType}").ConfigureAwait(true);

			// HTTP 1.1 mode (keep-alive not supported yet)
			await responseWriter.WriteLineAsync("Connection: close").ConfigureAwait(true);

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
						await responseWriter.WriteFormattedLineAsync($"{header.Key}: {value}").ConfigureAwait(true);
					}
				}
			}
		}

		private async Task ProcessResponsePayload(CancellationToken ct, TextWriter responseWriter, Stream responseStream)
		{
			if (_responseStreamFactory != null)
			{
				using (var streamToSend = await _responseStreamFactory(ct).ConfigureAwait(true))
				{
					// Content-Length header
					await responseWriter.WriteFormattedLineAsync($"Content-Length: {streamToSend.Length}").ConfigureAwait(true);
					await responseWriter.WriteLineAsync().ConfigureAwait(true);

					// Ensure header is flushed before writing to inner stream directly
					await responseWriter.FlushAsync().ConfigureAwait(true);

					// Write the stream content to inner stream
					await streamToSend.CopyToAsync(responseStream, 2048, ct).ConfigureAwait(false);
				}
			}
			else
			{
				var bytes = Utf8.GetBytes(_responseContent ?? string.Empty);

				// Content-Length header
				await responseWriter.WriteFormattedLineAsync($"Content-Length: {bytes.Length}").ConfigureAwait(false);
				await responseWriter.WriteLineAsync().ConfigureAwait(false);

				// Ensure header is flushed before writing to inner stream directly
				await responseWriter.FlushAsync().ConfigureAwait(false);

				// Write the stream content to inner stream
				await responseStream.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
			}
		}

		private static readonly char[] HeaderSeparator = [':'];

		private (string? header, string? content) ParseHeader(string line)
		{
			var parts = line.Split(HeaderSeparator, 2);
			if (parts.Length != 2)
			{
				return default;
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

		/// <summary>
		/// The method used in the request (GET, POST, PUT, DELETE, ...)
		/// </summary>
		public string Method { get; private set; } = string.Empty;

		/// <summary>
		/// The path requested by the user-agent. This is the path part of the url.
		/// </summary>
		public string Path { get; private set; } = string.Empty;

		/// <summary>
		/// The HTTP protocol used.
		/// </summary>
		public string? Http { get; private set; } = string.Empty;

		/// <summary>
		/// The "Host" header sent by the user-agent. May contain the port number.
		/// </summary>
		public string? Host { get; private set; } = "0.0.0.0";

		/// <summary>
		/// The "hostname" part of the "Host" header. Without the port number, if any.
		/// </summary>
		public string? HostName { get; private set; } = "0.0.0.0";

		/// <summary>
		/// The port number on which the server received the connection.
		/// </summary>
		public int Port { get; }

		/// <summary>
		/// The "Referer" header sent by the user-agent.
		/// </summary>
		public string? Referer { get; private set; }

		/// <summary>
		/// The "User-Agent" header sent by the user-agent.
		/// </summary>
		public string? UserAgent { get; private set; }

		/// <summary>
		/// The "Accept" header sent by the user-agent.
		/// </summary>
		public string? ContentType { get; private set; }

		/// <summary>
		/// The "Content-Length" header sent by the user-agent.
		/// </summary>
		public int? ContentLength { get; private set; }

		/// <summary>
		/// The body of the request, if any.
		/// </summary>
		public string? Body { get; private set; }

		/// <summary>
		/// The "Accept" header sent by the user-agent.
		/// </summary>
		public string? Accept { get; private set; }

		public Uri Url => _url ??= new Uri("http://" + Host + Path, UriKind.Absolute);

		public IReadOnlyDictionary<string, IReadOnlyCollection<string>>? Headers { get; private set; }

		public void SetResponse(
			string contentType,
			Func<CancellationToken, Task<Stream>> streamFactory,
			uint resultCode = 200,
			string resultText = "OK",
			IReadOnlyDictionary<string, IReadOnlyCollection<string>>? headers = null)
		{
			_responseContentType = contentType;
			_responseResultCode = resultCode;
			_responseResultText = resultText;
			_responseStreamFactory = streamFactory;
			_responseHeaders = headers;

			IsResponseSet = true;
		}

		public void SetResponse(
			string contentType,
			string content,
			uint resultCode = 200,
			string resultText = "OK",
			IReadOnlyDictionary<string, IReadOnlyCollection<string>>? headers = null)
		{
			_responseContentType = contentType;
			_responseResultCode = resultCode;
			_responseResultText = resultText;
			_responseContent = content;
			_responseHeaders = headers;

			IsResponseSet = true;
		}

		private string? _responseContentType;
		private uint? _responseResultCode;
		private string? _responseResultText;
		private Func<CancellationToken, Task<Stream>>? _responseStreamFactory;
		private string? _responseContent;
		private IReadOnlyDictionary<string, IReadOnlyCollection<string>>? _responseHeaders;
		private Uri? _url;

		internal bool IsResponseSet { get; private set; }

		public void Dispose() => _tcpClient.Dispose();
	}
}
