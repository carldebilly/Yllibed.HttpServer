using System;
using System.Collections.Generic;
using Yllibed.HttpServer.Sse;

namespace Yllibed.HttpServer.Handlers;

/// <summary>
/// Base handler for Server-Sent Events (SSE) endpoints.
/// Implement <see cref="HandleSseSession"/> and optionally override <see cref="ShouldHandle"/>, <see cref="GetHeaders"/>, <see cref="GetOptions"/>.
/// </summary>
public abstract class SseHandler : IHttpHandler
{
	/// <summary>
	/// Determines whether this handler should take ownership of the request and start an SSE session.
	/// Default filters to GET requests only.
	/// </summary>
	protected virtual bool ShouldHandle(IHttpServerRequest request, string relativePath)
		=> string.Equals(request.Method, "GET", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Validates the request's headers. Default checks that the Accept header allows "text/event-stream" (via Accept negotiation).
	/// Override this to customize Accept/content-type validation; ShouldHandle is intended for method/path filtering.
	/// </summary>
	protected virtual bool ValidateHeaders(IHttpServerRequest request) => request.ValidateAccept("text/event-stream");

	/// <summary>
	/// Optional extra headers for the SSE response (Content-Type and Connection are controlled; Cache-Control: no-cache is added unless overridden).
	/// </summary>
	protected virtual IReadOnlyDictionary<string, IReadOnlyCollection<string>>? GetHeaders(IHttpServerRequest request, string relativePath) => null;

	/// <summary>
	/// Optional SSE options (auto-heartbeat, auto-flush, etc.).
	/// </summary>
	protected virtual SseOptions? GetOptions(IHttpServerRequest request, string relativePath) => null;

	/// <summary>
	/// Implement the logic of your SSE session here. Use <paramref name="sse"/> to send events/comments.
	/// </summary>
	protected abstract Task HandleSseSession(ISseSession sse, CancellationToken ct);

	public Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
	{
		if (!ShouldHandle(request, relativePath))
		{
			return Task.CompletedTask;
		}

		if (!ValidateHeaders(request))
		{
			request.SetResponse("text/plain", "Not Acceptable", 406, "Not Acceptable");
			return Task.CompletedTask;
		}

		request.StartSseSession(HandleSseSession, headers: GetHeaders(request, relativePath), options: GetOptions(request, relativePath));

		return Task.CompletedTask;
	}
}
