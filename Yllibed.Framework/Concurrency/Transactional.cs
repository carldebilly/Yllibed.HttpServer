using System;
using System.Threading;

namespace Yllibed.Framework.Concurrency
{
	public static partial class Transactional
	{
		/// <summary>
		/// Transactionally updates the original reference using the provided selector
		/// </summary>
		/// <typeparam name="T">The type of the reference to update</typeparam>
		/// <param name="original">A ref variable to the original value</param>
		/// <param name="selector">A selector method that creates an updated version of the original value</param>
		/// <returns>Successful updated version</returns>
		public static T Update<T>(ref T original, Func<T, T> selector)
			 where T : class
		{
			do
			{
				// Get the original value
				var capture = original;

				// Apply the transformation using the selector
				var updated = selector(capture);

				// Compare and exchange the original with the updated value, if the original value has not changed.
				if (Interlocked.CompareExchange(ref original, updated, capture) == capture)
				{
					return updated;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the original reference using the provided selector
		/// </summary>
		/// <remarks>
		/// This version let you pass a parameter to prevent creation of a display class for capturing data in your lambda
		/// </remarks>
		/// <typeparam name="T">The type of the reference to update</typeparam>
		/// <param name="original">A ref variable to the original value</param>
		/// <param name="selector">A selector method that creates an updated version of the original value</param>
		/// <returns>Successful updated version</returns>
		public static T Update<T, TParam>(ref T original, TParam param, Func<T, TParam, T> selector)
			 where T : class
		{
			do
			{
				// Get the original value
				var capture = original;

				// Apply the transformation using the selector
				var updated = selector(capture, param);

				// Compare and exchange the original with the updated value, if the original value has not changed.
				if (Interlocked.CompareExchange(ref original, updated, capture) == capture)
				{
					return updated;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the original reference using the provided selector
		/// </summary>
		/// <remarks>
		/// This version let you pass a parameters to prevent creation of a display class for capturing data in your lambda
		/// </remarks>
		/// <typeparam name="T">The type of the reference to update</typeparam>
		/// <param name="original">A ref variable to the original value</param>
		/// <param name="selector">A selector method that creates an updated version of the original value</param>
		/// <returns>Successful updated version</returns>
		public static T Update<T, TParam1, TParam2>(ref T original, TParam1 param1, TParam2 param2, Func<T, TParam1, TParam2, T> selector)
			 where T : class
		{
			do
			{
				// Get the original value
				var capture = original;

				// Apply the transformation using the selector
				var updated = selector(capture, param1, param2);

				// Compare and exchange the original with the updated value, if the original value has not changed.
				if (Interlocked.CompareExchange(ref original, updated, capture) == capture)
				{
					return updated;
				}
			}
			while (true);
		}

		/// <summary>
		/// Transactionally updates the original reference using the provided selector, and returns an inner value from the selector.
		/// </summary>
		/// <typeparam name="TSource">The type of the reference to update</typeparam>
		/// <typeparam name="TResult">The inner value from the updated TSource returned by the selector</typeparam>
		/// <param name="original">The original value reference</param>
		/// <param name="selector">The selector </param>
		/// <returns>The inner value returned by the selector.</returns>
		public static TResult Update<TSource, TResult>(ref TSource original, Func<TSource, (TSource original, TResult result)> selector)
			 where TSource : class
		{
			do
			{
				var capture = original;

				var updated = selector(capture);

				if (Interlocked.CompareExchange(ref original, updated.original, capture) == capture)
				{
					return updated.result;
				}
			}
			while (true);
		}
	}
}
