using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Yllibed.HttpServer.Sse;

namespace Yllibed.HttpServer.Json;

public static class SseJsonExtensions
{
	/// <summary>
	/// Serializes the payload as JSON and sends it as an SSE message data.
	/// </summary>
	public static Task SendJsonAsync(this ISseSession sse, object? payload, string? eventName = null, string? id = null, CancellationToken ct = default)
	{
		var json = JsonConvert.SerializeObject(payload, Formatting.None);
		return sse.SendEventAsync(json, eventName: eventName, id: id, ct: ct);
	}

	/// <summary>
	/// Helper alias for SendJsonAsync to emphasize event name parameter first.
	/// </summary>
	public static Task SendJsonEventAsync(this ISseSession sse, string eventName, object? payload, string? id = null, CancellationToken ct = default)
		=> SendJsonAsync(sse, payload, eventName: eventName, id: id, ct: ct);
}
