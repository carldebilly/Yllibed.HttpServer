using System.Collections.Generic;
using System.Linq;

namespace Yllibed.HttpServer.Extensions;

public static class UriExtensions
{
	private static readonly char[] _uriSplitChars = new char[2] { '?', '&' };
	public static IDictionary<string, string> GetParameters(this Uri uri)
	{

		return (from p in uri.OriginalString.Split(_uriSplitChars, StringSplitOptions.RemoveEmptyEntries)
				select p.Split('=') into parts
				where parts.Length > 1
				select parts).ToDictionary((parts) => parts[0], (parts) => string.Join("=", parts.Skip(1)), StringComparer.Ordinal);
	}
}
