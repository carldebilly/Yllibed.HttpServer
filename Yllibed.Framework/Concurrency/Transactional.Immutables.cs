using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Yllibed.Framework.Concurrency
{
	partial class Transactional
	{
		#region ImmutableDictionary
		/// <summary>
		/// Transactionally get or add an item to an ImmutableDictionary.  The factory is called to create the item when required.
		/// </summary>
		public static TValue GetOrAdd<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			Func<TKey, TValue> factory)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			while (true)
			{
				var capture = dictionary;
				TValue value;

				if (capture.TryGetValue(key, out value))
				{
					return value;
				}

				value = factory(key);
				var updated = (TDictionary)capture.Add(key, value);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return value; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally get or add an item to an ImmutableDictionary.  The factory is called to create the item when required.
		/// </summary>
		public static TValue SetItem<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			Func<TKey, TValue> factory)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			while (true)
			{
				var capture = dictionary;

				var value = factory(key);
				var updated = (TDictionary)capture.SetItem(key, value);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return value; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally get or add an item to an ImmutableDictionary.  The factory is called to create the item when required.
		/// </summary>
		public static TValue UpdateItem<TDictionary, TKey, TValue>(
			ref TDictionary dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
			where TDictionary : class, IImmutableDictionary<TKey, TValue>
		{
			while (true)
			{
				var capture = dictionary;

				var value = factory(key, capture.GetValueOrDefault(key));
				var updated = (TDictionary)capture.SetItem(key, value);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return value; // successfully updated the dictionary
				}
			}
		}

		/// <summary>
		/// Transactionally get or add an item to an ImmutableDictionary.  The factory is called to create the item when required.
		/// </summary>
		public static TValue UpdateItem<TKey, TValue>(
			ref IImmutableDictionary<TKey, TValue> dictionary,
			TKey key,
			Func<TKey, TValue, TValue> factory)
		{
			while (true)
			{
				var capture = dictionary;

				var value = factory(key, capture.GetValueOrDefault(key));
				var updated = capture.SetItem(key, value);

				if (Interlocked.CompareExchange(ref dictionary, updated, capture) == capture)
				{
					return value; // successfully updated the dictionary
				}
			}
		}
		#endregion

		#region ImmutableQueue
		/// <summary>
		/// Transactionally enqueue and item into an ImmutableQueue
		/// </summary>
		public static void Enqueue<TQueue, T>(ref TQueue queue, T value)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				var updated = (TQueue)capture.Enqueue(value);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					return; // successfully updated the queue
				}
			}
		}

		/// <summary>
		/// Transactionally enqueue and item into an ImmutableQueue
		/// </summary>
		public static T Enqueue<TQueue, T>(ref TQueue queue, Func<TQueue, T> valueFactory)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				var value = valueFactory(capture);
				var updated = (TQueue)capture.Enqueue(value);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					return value; // successfully updated the queue
				}
			}
		}

		/// <summary>
		/// Transactionally dequeue an item from a queue.
		/// </summary>
		/// <returns>true if successful, false means queue was empty</returns>
		public static bool TryDequeue<TQueue, T>(ref TQueue queue, out T value)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				if (capture.IsEmpty)
				{
					value = default(T);
					return false;
				}

				T output;
				var updated = (TQueue)capture.Dequeue(out output);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					value = output;
					return true; // successfully updated the queue
				}
			}
		}

		/// <summary>
		/// Transactionally dequeue an item from a queue. An exception is thrown if queue is empty.
		/// </summary>
		/// <returns>dequeued item</returns>
		public static T Dequeue<TQueue, T>(ref TQueue queue)
			where TQueue : class, IImmutableQueue<T>
		{
			while (true)
			{
				var capture = queue;

				T output;
				var updated = (TQueue)capture.Dequeue(out output);

				if (Interlocked.CompareExchange(ref queue, updated, capture) == capture)
				{
					return output; // successfully updated the queue
				}
			}
		}
		#endregion

		#region ImmutableList
		/// <summary>
		/// Transactionally remove an item from a list.
		/// </summary>
		/// <returns>True if the item was in the list, false else.</returns>
		public static bool Remove<TList, T>(ref TList list, T item)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				var updated = (TList)capture.Remove(item);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return capture.Contains(item);
				}
			}
		}

		/// <summary>
		/// Transactionally remove the specified items from a list.
		/// </summary>
		/// <returns>Number of items which were effectively removed from the list.</returns>
		public static int RemoveRange<TList, T>(ref TList list, T[] items)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				var updated = (TList)capture.RemoveRange(items);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					return capture.Count - updated.Count;
				}
			}
		}

		/// <summary>
		/// Transactionally remove the specified items from a list.
		/// </summary>
		/// <param name="removedItems">Items which were effectively removed from the list.</param>
		/// <returns>Number of items which were effectively removed from the list.</returns>
		public static int RemoveRange<TList, T>(ref TList list, T[] items, out IEnumerable<T> removedItems)
			where TList : class, IImmutableList<T>
		{
			while (true)
			{
				var capture = list;
				var updated = (TList)capture.RemoveRange(items);

				if (Interlocked.CompareExchange(ref list, updated, capture) == capture)
				{
					var removed = capture.Count - updated.Count;
					removedItems = removed > 0 
						? items.Where(capture.Contains)
						: Enumerable.Empty<T>();
					return removed;
				}
			}
		}
		#endregion
	}
}
