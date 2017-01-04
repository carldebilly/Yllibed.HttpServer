using System;
using System.Reactive.Concurrency;
using System.Threading;

namespace Yllibed.Framework.Concurrency
{
	internal class SchedulerSynchronizationContext : SynchronizationContext
	{
		private readonly IScheduler _source;

		public SchedulerSynchronizationContext(IScheduler source)
		{
			_source = source;
		}

		public Exception Exception { get; private set; }

		public override void Post(SendOrPostCallback d, object state)
		{
			_source.Schedule(() => d(state));
		}

		public override void Send(SendOrPostCallback d, object state)
		{
			try
			{
				d(state);
			}
			catch (Exception e)
			{
				Exception = e;
			}
		}
	}
}
