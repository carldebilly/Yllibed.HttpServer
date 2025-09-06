namespace Yllibed.HttpServer.Sse;

#pragma warning disable MA0001 // Use an overload of 'Replace' that has a StringComparison parameter

/// <summary>
/// Utilities to format and write Server-Sent Events (SSE) frames.
/// See WHATWG HTML LS: https://html.spec.whatwg.org/multipage/server-sent-events.html#server-sent-events
/// </summary>
internal static class SseHelper
{
	/// <summary>
	/// Writes a single SSE event to the provided writer.
	/// </summary>
	/// <param name="writer">The response writer (its NewLine should generally be "\n").</param>
	/// <param name="data">The event payload (can be multi-line; each line will be prefixed with "data:").</param>
	/// <param name="eventName">Optional event name ("event:").</param>
	/// <param name="id">Optional event id ("id:").</param>
	/// <param name="ct">Cancellation token (checked before/after writes).</param>
	public static async Task WriteEvent(TextWriter writer, string data, string? eventName = null, string? id = null, CancellationToken ct = default)
	{
		if (writer is null) throw new ArgumentNullException(nameof(writer));

		ct.ThrowIfCancellationRequested();

		if (!string.IsNullOrEmpty(id))
		{
			await writer.WriteLineAsync($"id: {id}").ConfigureAwait(false);
		}
		if (!string.IsNullOrEmpty(eventName))
		{
			await writer.WriteLineAsync($"event: {eventName}").ConfigureAwait(false);
		}

		if (data is null)
		{
			data = string.Empty;
		}

		// Write each line prefixed with "data: "
		var sb = new StringBuilder(data.Length);
		for (var idx = 0; idx < data.Length; idx++)
		{
			var ch = data[idx];
			if (ch == '\r')
			{
				if (idx + 1 < data.Length && data[idx + 1] == '\n')
				{
					continue; // skip the '\r' of CRLF
				}

				sb.Append('\n');
			}
			else
			{
				sb.Append(ch);
			}
		}

		var normalized = sb.ToString();
		var parts = normalized.Split('\n');
		foreach (var line in parts)
		{
			await writer.WriteLineAsync($"data: {line}").ConfigureAwait(false);
		}

		// Terminate the event with a blank line
		await writer.WriteLineAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Writes a comment heartbeat frame. Many proxies/timeouts are avoided by sending these periodically.
	/// </summary>
	/// <param name="writer">The response writer.</param>
	/// <param name="comment">Comment text after the colon (no semantics to client).</param>
	/// <param name="ct">Cancellation token (checked before/after writes).</param>
	public static async Task WriteComment(TextWriter writer, string comment = "keepalive", CancellationToken ct = default)
	{
		if (writer is null) throw new ArgumentNullException(nameof(writer));
		ct.ThrowIfCancellationRequested();
		await writer.WriteLineAsync($": {comment}").ConfigureAwait(false);
		await writer.WriteLineAsync().ConfigureAwait(false);
	}
}
