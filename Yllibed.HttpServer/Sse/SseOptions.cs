namespace Yllibed.HttpServer.Sse;

/// <summary>
/// Options for SSE sessions.
/// </summary>
/// <remarks>
/// - Heartbeats: While not mandated by spec, periodic comments help keep intermediaries from timing out idle connections.
///   See WHATWG SSE processing model.
/// </remarks>
public sealed class SseOptions
{
	/// <summary>
	/// The interval between heartbeat comments.
	/// </summary>
	/// <remarks>
	/// The default value is 45 seconds.
	/// </remarks>
	public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(45);

	/// <summary>
	/// The comment sent in the heartbeat.
	/// </summary>
	public string HeartbeatComment { get; set; } = "keepalive";

	/// <summary>
	/// If true, the server will automatically flush the output stream after each message.
	/// </summary>
	/// <remarks>
	/// The default value is true.
	/// </remarks>
	public bool AutoFlush { get; set; } = true;
}
