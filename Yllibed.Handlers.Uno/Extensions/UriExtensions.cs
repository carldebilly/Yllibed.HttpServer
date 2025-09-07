using System.Collections.Generic;
using System.Linq;

namespace Yllibed.Handlers.Uno.Extensions;

public static class UriExtensions
{
	private static readonly char[] _uriSplitChars = new char[2] { '?', '&' };
	public static IDictionary<string, string> GetParameters(this Uri uri)
	{

		return uri
				.OriginalString
				.Split(_uriSplitChars, StringSplitOptions.RemoveEmptyEntries)
				.Select(p => p.Split('='))
				.Where(parts => parts.Length > 1)
				.ToDictionary(parts => parts[0], parts => string.Join('=', parts.Skip(1)), StringComparer.Ordinal);
	}
}
