using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace Yllibed.Handlers.Uno.Extensions;

public static class UriExtensions
{
	private static readonly char[] _uriSplitChars = new char[2] { '?', '&' };
	public static IDictionary<string, string> GetParameters(this Uri uri)
	{

		return uri
				.Query
				.Split(_uriSplitChars, StringSplitOptions.RemoveEmptyEntries)
				.Select(p => p.Split('='))
				.Where(parts => parts.Length > 1)
				.ToDictionary(parts => parts[0], parts => string.Join('=', parts.Skip(1)), StringComparer.Ordinal);
	}
	public static NameValueCollection GetQuery(this Uri? redirectUri, Uri callbackUri) // TODO: Check if we maybe should exchange using GetParameters to this method
	{
		if (redirectUri is null)
			return [];
		return redirectUri.IsBaseOf(callbackUri) // Reused from Uno.Extensions.Authentication.Web.WebAuthenticationProvider and changed to use Uri instead of string
			 ? AuthHttpUtility.ExtractArguments(redirectUri.ToString())  // it's a fully qualified url, so need to extract query or fragment
			 : AuthHttpUtility.ParseQueryString(redirectUri.ToString().TrimStart('#').TrimStart('?')); // it isn't a full url, so just process as query or fragment

	}
}
