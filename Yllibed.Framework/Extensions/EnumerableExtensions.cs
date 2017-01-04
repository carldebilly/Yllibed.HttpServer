using System.Collections.Generic;
using System.Linq;

namespace Yllibed.Framework.Extensions
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Check if an enumerable is null or empty (works on strings, too)
		/// </summary>
		public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
		{
			return source == null || !source.Any();
		}
	}
}
