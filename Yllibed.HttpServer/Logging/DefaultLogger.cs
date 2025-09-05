using Microsoft.Extensions.Logging;

namespace Yllibed.Framework.Logging;

public static class DefaultLogger
{
	public static ILoggerFactory LoggerFactory { get; }

	static DefaultLogger() => LoggerFactory = new LoggerFactory();
}
