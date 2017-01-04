using System.Globalization;

namespace Yllibed.Framework.Extensions
{
	public static class StringExtensions
	{
		public static bool IsNullOrWhiteSpace(this string str)
		{
			return string.IsNullOrWhiteSpace(str);
		}

		public static string InvariantCultureFormat(this string instance, params object[] array)
		{
			return string.Format(CultureInfo.InvariantCulture, instance, array);
		}
	}
}
