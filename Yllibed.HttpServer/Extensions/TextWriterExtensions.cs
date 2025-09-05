using System.Globalization;
using System.Runtime.CompilerServices;

namespace Yllibed.HttpServer.Extensions;

#pragma warning disable MA0045 // Don't force using ct here

internal static class TextWriterExtensions
{
	public static Task WriteFormattedLineAsync(this TextWriter writer, FormattableString str) => writer.WriteLineAsync(str.ToString(CultureInfo.InvariantCulture));

	public static Task WriteFormattedAsync(this TextWriter writer, FormattableString str) => writer.WriteAsync(str.ToString(CultureInfo.InvariantCulture));

	public static void WriteFormattedLine(this TextWriter writer, FormattableString str) => writer.WriteLine(str.ToString(CultureInfo.InvariantCulture));

	public static void WriteFormatted(this TextWriter writer, FormattableString str) => writer.Write(str.ToString(CultureInfo.InvariantCulture));

#if NETSTANDARD2_0
#pragma warning disable MA0040

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task FlushAsync(this TextWriter writer, CancellationToken ct = default) => writer.FlushAsync();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Task<string> ReadLineAsync(this TextReader reader, CancellationToken ct = default) => reader.ReadLineAsync();
#endif
}
