using System;
using System.Reactive.Disposables;
using System.Threading;

namespace Yllibed.Framework.Concurrency
{
	public static class SynchronizationContextHelper
	{
		/// <summary>
		/// Sets the provided synchronization context as the current, restores the previous one when disposed.
		/// </summary>
		public static IDisposable ScopedSet(SynchronizationContext context)
		{
			var current = SynchronizationContext.Current;

			SynchronizationContext.SetSynchronizationContext(context);

			return Disposable.Create(() => SynchronizationContext.SetSynchronizationContext(current));
		}
	}
}
