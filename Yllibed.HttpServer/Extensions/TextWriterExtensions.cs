using System;
using System.IO;
using System.Threading.Tasks;

namespace Yllibed.HttpServer.Extensions
{
	public static class TextWriterExtensions
	{
		public static Task WriteFormattedLineAsync(this TextWriter writer, FormattableString str)
		{
			return writer.WriteLineAsync(str.ToString(writer.FormatProvider));
		}

		public static Task WriteFormattedAsync(this TextWriter writer, FormattableString str)
		{
			return writer.WriteAsync(str.ToString(writer.FormatProvider));
		}

		public static void WriteFormattedLine(this TextWriter writer, FormattableString str)
		{
			writer.WriteLine(str.ToString(writer.FormatProvider));
		}

		public static void WriteFormatted(this TextWriter writer, FormattableString str)
		{
			writer.Write(str.ToString(writer.FormatProvider));
		}
	}
}
