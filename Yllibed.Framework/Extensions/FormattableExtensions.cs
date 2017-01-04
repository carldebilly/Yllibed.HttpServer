using System;
using System.Globalization;

namespace Yllibed.Framework.Extensions
{
	public static class FormattableExtensions
	{
		public static string ToStringInvariant<T>(this T formattable) where T : IFormattable
		{
			return formattable.ToString(null, CultureInfo.InvariantCulture);
		}

		public static string InvariantToString(this FormattableString str)
		{
			return str.ToString(CultureInfo.InvariantCulture);
		}
	}
}
