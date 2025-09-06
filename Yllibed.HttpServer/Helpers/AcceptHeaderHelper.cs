using System;
using System.Globalization;

namespace Yllibed.HttpServer.Helpers;

internal static class AcceptHeaderHelper
{
	/// <summary>
	/// Validates if the provided Accept header allows the specified media type.
	/// </summary>
	/// <remarks>
	/// Accepts if:
	/// - Accept header is missing or empty;
	/// - */* is present with q>0;
	/// - type/* matches the media type's type with q>0;
	/// - exact media type matches (case-insensitive) with q>0.
	/// q-parameter default is 1 when omitted; q=0 means "not acceptable" for that media range.
	///
	/// Specification reference:
	/// - RFC 7231 section 5.3.2 (Accept): https://tools.ietf.org/html/rfc7231#section-5.3.2
	/// - RFC 7231 section 5.3.1 (Quality Values): https://tools.ietf.org/html/rfc7231#section-5.3.1
	/// </remarks>
	public static bool IsAccepted(string? acceptHeader, string mediaType)
	{
		if (string.IsNullOrWhiteSpace(mediaType))
		{
			throw new ArgumentNullException(nameof(mediaType));
		}

		if (string.IsNullOrWhiteSpace(acceptHeader))
		{
			return true; // no constraint
		}

		// Split mediaType into type/subtype (manual scan to avoid analyzer complaints)
		var slashIdx = -1;
		for (var i = 0; i < mediaType.Length; i++)
		{
			if (mediaType[i] == '/')
			{
				slashIdx = i; break;
			}
		}
		var typePart = slashIdx > 0 ? mediaType[..slashIdx] : mediaType;

		var header = acceptHeader ?? string.Empty;
		foreach (var part in header.Split(','))
		{
			var token = part.Trim();
			if (token.Length == 0) continue;

			// Parse parameters (e.g., ;q=0.9)
			var q = 1.0; // default quality
			var mediaRange = token;
			var semi = -1;
			for (var i = 0; i < token.Length; i++)
			{
				if (token[i] == ';')
				{
					semi = i; break;
				}
			}
			if (semi >= 0)
			{
				mediaRange = token[..semi].Trim();
				var paramsPart = token[(semi + 1)..];
				foreach (var pv in paramsPart.Split(';'))
				{
					var p = pv.Trim();
					if (p.Length == 0) continue;
					var eq = -1;
					for (var j = 0; j < p.Length; j++)
					{
						if (p[j] == '=')
						{
							eq = j; break;
						}
					}
					if (eq <= 0) continue;
					var name = p[..eq].Trim();
					var value = p[(eq + 1)..].Trim();
					if (name.Equals("q", StringComparison.OrdinalIgnoreCase))
					{
						if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var qv))
						{
							q = qv;
						}
					}
				}
			}

			if (q <= 0)
			{
				continue; // not acceptable for this range
			}

			if (mediaRange.Equals("*/*", StringComparison.Ordinal))
			{
				return true;
			}

			// Find slash in mediaRange
			var slash = -1;
			for (var i = 0; i < mediaRange.Length; i++)
			{
				if (mediaRange[i] == '/')
				{
					slash = i; break;
				}
			}
			if (slash > 0)
			{
				var t = mediaRange[..slash];
				var s = mediaRange[(slash + 1)..];
				if (s.Equals("*", StringComparison.Ordinal))
				{
					if (t.Equals(typePart, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}

			if (mediaRange.Equals(mediaType, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}
