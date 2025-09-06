namespace Yllibed.HttpServer.Sse;

/// <summary>
/// Represents an active SSE session used by application handlers to emit events.
/// </summary>
/// <remarks>
/// Framing per WHATWG HTML Living Standard (Server-Sent Events):
/// https://html.spec.whatwg.org/multipage/server-sent-events.html#server-sent-events
/// </remarks>
public interface ISseSession
{
	/// <summary>Original HTTP request that initiated this SSE session.</summary>
	IHttpServerRequest Request { get; }

	/// <summary>Indicates whether the session is still considered connected (based on cancellation state).</summary>
	bool IsConnected { get; }

	/// <summary>
	/// The SSE Last-Event-ID header value sent by the client for this session, if any.
	/// </summary>
	string? LastEventId { get; }

	/// <summary>
	/// Sends an event to the client.
	/// </summary>
	Task SendEventAsync(string data, string? eventName = null, string? id = null, CancellationToken ct = default);

	/// <summary>
	/// Sends a comment frame to the client.
	/// </summary>
	/// <remarks>
	/// Comments are not processed by the client, they are mostly used to keep the connection alive or to
	/// help developers to debug their application.
	/// </remarks>
	Task SendCommentAsync(string comment, CancellationToken ct = default);

	/// <summary>
	/// Flushes the output buffer to the client.
	/// </summary>
	Task FlushAsync(CancellationToken ct = default);
}
