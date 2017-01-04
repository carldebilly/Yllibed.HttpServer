﻿using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Yllibed.Framework.Logging
{
	public static class LogExtensions
	{
		// Helper class to cache the creation of the logger instance
		private static class LoggerOfT<T>
		{
			internal static readonly ILogger LoggerInstance = DefaultLogger.LoggerFactory.CreateLogger(typeof(T).FullName);
		}

		public static ILogger Log<T>(this T source)
		{
			return LoggerOfT<T>.LoggerInstance;
		}

		public static void Error(this ILogger logger, FormattableString str)
		{
			logger.LogError(str.ToString(CultureInfo.InvariantCulture));
		}

		public static void Info(this ILogger logger, FormattableString str)
		{
			logger.LogInformation(str.ToString(CultureInfo.InvariantCulture));
		}
	}
}