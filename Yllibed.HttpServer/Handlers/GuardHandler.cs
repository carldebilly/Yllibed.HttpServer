using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Yllibed.HttpServer.Handlers;

/// <summary>
/// A basic request guard that performs best-effort filtering of incoming requests,
/// rejecting them early when violating basic limits (URL length, headers size/count, payload size, etc.).
/// This provides lightweight filtering against unsophisticated attacks, not comprehensive security.
/// Place it first in the pipeline. Can optionally wrap an inner handler (next).
/// </summary>
public sealed class GuardHandler : IHttpHandler
{
	public sealed class Options
	{
		public int? MaxUrlLength { get; set; } = 2048;
		public int? MaxHeadersCount { get; set; } = 100;
		public int? MaxHeadersTotalSize { get; set; } = 32 * 1024;
		public int? MaxBodyBytes { get; set; } = 10 * 1024 * 1024;
		public string[]? AllowedMethods { get; set; } = new[] { "GET", "POST" };
		public string[]? AllowedHosts { get; set; } = null; // null = any
		public bool RequireHostHeader { get; set; } = true;
	}

	public int? MaxUrlLength { get; }
	public int? MaxHeadersCount { get; }
	public int? MaxHeadersTotalSize { get; }
	public int? MaxBodyBytes { get; }
	public bool RequireHostHeader { get; }

	private readonly HashSet<string>? _allowedMethods;
	private readonly HashSet<string>? _allowedHosts;
	private readonly IHttpHandler? _inner;

	/// <summary>
	/// Create a new GuardHandler with optional limits. Null means no limit for that dimension.
	/// Defaults are conservative and can be adjusted per app needs.
	/// </summary>
	/// <param name="maxUrlLength">Max allowed length of Path (including query). Default 2048.</param>
	/// <param name="maxHeadersCount">Max allowed number of request headers. Default 100.</param>
	/// <param name="maxHeadersTotalSize">Max cumulative size of header keys+values (characters). Default 32k.</param>
	/// <param name="maxBodyBytes">Max payload size from Content-Length. Default 10 MB.</param>
	/// <param name="allowedMethods">Optional allowed HTTP methods (case-insensitive). Null = any.</param>
	/// <param name="allowedHosts">Optional allowed Host header values (case-insensitive). Null = any.</param>
	/// <param name="requireHostHeader">Require Host header to be present (default true).</param>
	/// <param name="inner">Optional inner (next) handler to call when checks pass.</param>
	public GuardHandler(
		int? maxUrlLength = 2048,
		int? maxHeadersCount = 100,
		int? maxHeadersTotalSize = 32 * 1024,
		int? maxBodyBytes = 10 * 1024 * 1024,
		IEnumerable<string>? allowedMethods = null,
		IEnumerable<string>? allowedHosts = null,
		bool requireHostHeader = true,
		IHttpHandler? inner = null)
	{
		MaxUrlLength = maxUrlLength;
		MaxHeadersCount = maxHeadersCount;
		MaxHeadersTotalSize = maxHeadersTotalSize;
		MaxBodyBytes = maxBodyBytes;
		_allowedMethods = allowedMethods != null ? new HashSet<string>(allowedMethods, StringComparer.OrdinalIgnoreCase) : null;
		_allowedHosts = allowedHosts != null ? new HashSet<string>(allowedHosts, StringComparer.OrdinalIgnoreCase) : null;
		RequireHostHeader = requireHostHeader;
		_inner = inner;
	}

	/// <summary>
	/// DI-friendly constructor using Microsoft.Extensions.Options.IOptions.
	/// </summary>
	[Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
	public GuardHandler(Microsoft.Extensions.Options.IOptions<Options> options)
		: this(
			options?.Value?.MaxUrlLength,
			options?.Value?.MaxHeadersCount,
			options?.Value?.MaxHeadersTotalSize,
			options?.Value?.MaxBodyBytes,
			options?.Value?.AllowedMethods,
			options?.Value?.AllowedHosts,
			options?.Value?.RequireHostHeader ?? true,
			inner: null)
	{
	}

	public async Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
	{
		// 0) Method allow-list
		if (_allowedMethods is { Count: > 0 })
		{
			var method = request.Method?.ToUpperInvariant() ?? string.Empty;
			if (!_allowedMethods.Contains(method))
			{
				Reject(request, 405, "METHOD NOT ALLOWED", System.FormattableString.Invariant($"Method '{request.Method}' not allowed"));
				return;
			}
		}

		// 0.1) Host header presence and allow-list
		if (RequireHostHeader && string.IsNullOrWhiteSpace(request.Host))
		{
			Reject(request, 400, "BAD REQUEST", "Missing Host header");
			return;
		}
		if (_allowedHosts is { Count: > 0 } && request.Host is { Length: > 0 } host)
		{
			// Compare case-insensitively on full Host header (can include port)
			var allowed = _allowedHosts.Contains(host)
				|| (request.HostName is { Length: > 0 } hn && _allowedHosts.Contains(hn));
			if (!allowed)
			{
				Reject(request, 403, "FORBIDDEN", "Host not allowed");
				return;
			}
		}

		// 1) URL length (Path already includes querystring per IHttpServerRequest contract)
		if (MaxUrlLength is int maxUrl && request.Path is { Length: > 0 } path && path.Length > maxUrl)
		{
			Reject(request, 414, "URI TOO LONG", System.FormattableString.Invariant($"URI too long (limit: {maxUrl} chars)"));
			return;
		}

		// 2) Headers count / total size
		var headers = request.Headers;
		if (headers != null)
		{
			if (MaxHeadersCount is int maxCount && headers.Count > maxCount)
			{
				Reject(request, 431, "REQUEST HEADER FIELDS TOO LARGE", System.FormattableString.Invariant($"Too many headers (limit: {maxCount})"));
				return;
			}

			if (MaxHeadersTotalSize is int maxSize)
			{
				var total = 0;
				foreach (var kvp in headers)
				{
					// Approximate header size as key length + sum of values length
					total += kvp.Key.Length;
					total += kvp.Value.Sum(v => v?.Length ?? 0);
					if (total > maxSize)
					{
						break;
					}
				}
				if (total > maxSize)
				{
					Reject(request, 431, "REQUEST HEADER FIELDS TOO LARGE", System.FormattableString.Invariant($"Headers too large (limit: {maxSize} chars)"));
					return;
				}
			}
		}

		// 3) Payload (uses Content-Length when provided; chunked transfer isn't supported by this server)
		if (MaxBodyBytes is int maxBody && request.ContentLength is int len && len > maxBody)
		{
			Reject(request, 413, "PAYLOAD TOO LARGE", System.FormattableString.Invariant($"Payload too large (limit: {maxBody} bytes)"));
			return;
		}

		// 4) If wrapping another handler, pass-through
		if (_inner is not null)
		{
			await _inner.HandleRequest(ct, request, relativePath);
		}
	}

	private static void Reject(IHttpServerRequest request, uint status, string reason, string message)
	{
		request.SetResponse("text/plain", message, status, reason);
	}
}
