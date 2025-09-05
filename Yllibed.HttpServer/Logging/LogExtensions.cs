using Microsoft.Extensions.Logging;

namespace Yllibed.Framework.Logging;

public static class LogExtensions
{
	// Helper class to cache the creation of the logger instance
	private static class LoggerOfT<T>
	{
		internal static readonly ILogger LoggerInstance = DefaultLogger.LoggerFactory.CreateLogger(typeof(T).FullName!);
	}

	public static ILogger Log<T>(this T source) => LoggerOfT<T>.LoggerInstance;
}
