using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Yllibed.Framework.Extensions;

namespace Yllibed.Framework.Concurrency
{
	public static partial class SchedulerExtensions
	{
		/// <summary>
		/// Schedulers the specified task on the specified scheduler.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="taskBuilder"></param>
		/// <returns></returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static IDisposable Schedule(this IScheduler source, Func<Task> taskBuilder)
		{
			return Schedule(source, _ => taskBuilder());
		}

		/// <summary>
		/// Schedulers the specified task on the specified scheduler.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="taskBuilder"></param>
		/// <returns></returns>
		public static IDisposable Schedule(this IScheduler source, Func<CancellationToken, Task> taskBuilder)
		{
			var subscriptions = new CompositeDisposable(2);
			var cancellationDisposable = new CancellationDisposable();

			// Capture the source context before calling schedule.
			var sourceCtx = new SchedulerSynchronizationContext(source);

			// It is acceptable to use the async void pattern as long as the
			// synchronization context is not the default, i.e. executing on the thread pool,
			// where exceptions are not trapped properly.
			System.Reactive.Concurrency.Scheduler.Schedule(
				source,
				() =>
				{
					using (SynchronizationContextHelper.ScopedSet(sourceCtx))
					{
						taskBuilder(cancellationDisposable.Token)
							.ContinueWith(t =>
								{
									if (t.IsFaulted)
									{
										source.Schedule(() =>
										{
											throw t.Exception;
										});
									}
								}
							);
					}
				}
			).DisposeWith(subscriptions);

			cancellationDisposable.DisposeWith(subscriptions);

			return subscriptions;
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask(
			this IScheduler scheduler,
			Func<CancellationToken, IScheduler, Task> taskBuilder)
		{
			return scheduler.ScheduleTask(default(object), (ct, s, st) => taskBuilder(ct, scheduler));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask(
			this IScheduler scheduler,
			Func<CancellationToken, IScheduler, Task<IDisposable>> taskBuilder)
		{
			return scheduler.ScheduleTask(default(object), (ct, s, st) => taskBuilder(ct, scheduler));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="dueTime">Relative time after which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask(
			this IScheduler scheduler,
			TimeSpan dueTime,
			Func<CancellationToken, IScheduler, Task> taskBuilder)
		{
			return scheduler.ScheduleTask(default(object), dueTime, (ct, s, st) => taskBuilder(ct, scheduler));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="dueTime">Relative time after which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask(
			this IScheduler scheduler,
			TimeSpan dueTime,
			Func<CancellationToken, IScheduler, Task<IDisposable>> taskBuilder)
		{
			return scheduler.ScheduleTask(default(object), dueTime, (ct, s, st) => taskBuilder(ct, scheduler));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="dueTime">Absolute time at which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask(
			this IScheduler scheduler,
			DateTimeOffset dueTime,
			Func<CancellationToken, IScheduler, Task> taskBuilder)
		{
			return scheduler.ScheduleTask(default(object), dueTime, (ct, s, st) => taskBuilder(ct, scheduler));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="dueTime">Absolute time at which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask(
			this IScheduler scheduler,
			DateTimeOffset dueTime,
			Func<CancellationToken, IScheduler, Task<IDisposable>> taskBuilder)
		{
			return scheduler.ScheduleTask(default(object), dueTime, (ct, s, st) => taskBuilder(ct, scheduler));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="state">State to pass to the asynchronous method.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask<TState>(
			this IScheduler scheduler, TState state,
			Func<CancellationToken, IScheduler, TState, Task> taskBuilder)
		{
			var schedulerContext = new SchedulerSynchronizationContext(scheduler);
			return scheduler.Schedule(state, (s, st) => InvokeTask(scheduler, st, taskBuilder, schedulerContext));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="state">State to pass to the asynchronous method.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask<TState>(
			this IScheduler scheduler,
			TState state,
			Func<CancellationToken, IScheduler, TState, Task<IDisposable>> taskBuilder)
		{
			var schedulerContext = new SchedulerSynchronizationContext(scheduler);
				return scheduler.Schedule(state, (s, st) => InvokeTaskWithDisposable(scheduler, st, taskBuilder, schedulerContext));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="state">State to pass to the asynchronous method.</param>
		/// <param name="dueTime">Relative time after which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask<TState>(
			this IScheduler scheduler,
			TState state,
			TimeSpan dueTime,
			Func<CancellationToken, IScheduler, TState, Task> taskBuilder)
		{
			var schedulerContext = new SchedulerSynchronizationContext(scheduler);
				return scheduler.Schedule(state, dueTime, (s, st) => InvokeTask(scheduler, st, taskBuilder, schedulerContext));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="state">State to pass to the asynchronous method.</param>
		/// <param name="dueTime">Relative time after which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask<TState>(
			this IScheduler scheduler,
			TState state,
			TimeSpan dueTime,
			Func<CancellationToken, IScheduler, TState, Task<IDisposable>> taskBuilder)
		{
			var schedulerContext = new SchedulerSynchronizationContext(scheduler);
				return scheduler.Schedule(state, dueTime, (s, st) => InvokeTaskWithDisposable(scheduler, st, taskBuilder, schedulerContext));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="state">State to pass to the asynchronous method.</param>
		/// <param name="dueTime">Absolute time at which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask<TState>(
			this IScheduler scheduler,
			TState state,
			DateTimeOffset dueTime,
			Func<CancellationToken, IScheduler, TState, Task> taskBuilder)
		{
			var schedulerContext = new SchedulerSynchronizationContext(scheduler);
				return scheduler.Schedule(state, dueTime, (s, st) => InvokeTask(scheduler, st, taskBuilder, schedulerContext));
		}

		/// <summary>
		/// Schedules work using an asynchronous method, allowing for cooperative scheduling in an imperative coding style.
		/// </summary>
		/// <param name="scheduler">Scheduler to schedule work on.</param>
		/// <param name="state">State to pass to the asynchronous method.</param>
		/// <param name="dueTime">Absolute time at which to execute the action.</param>
		/// <param name="taskBuilder">Asynchronous method to run the work.</param>
		/// <returns>Disposable object that allows to cancel outstanding work on cooperative cancellation points or through the cancellation token passed to the asynchronous method.</returns>
		public static IDisposable ScheduleTask<TState>(
			this IScheduler scheduler,
			TState state,
			DateTimeOffset dueTime,
			Func<CancellationToken, IScheduler, TState, Task<IDisposable>> taskBuilder)
		{
			var schedulerContext = new SchedulerSynchronizationContext(scheduler);
				return scheduler.Schedule(state, dueTime,
					(s, st) => InvokeTaskWithDisposable(scheduler, st, taskBuilder, schedulerContext));
		}

		/// <summary>
		/// Awaits a task execution on the specified scheduler, providing the result.
		/// </summary>
		/// <returns>A task that will provide the result of the execution.</returns>
		public static Task<T> Run<T>(this IScheduler source, Func<CancellationToken, Task<T>> taskBuilder, CancellationToken cancellationToken)
		{
			var completion = new FastTaskCompletionSource<T>();

			var disposable = new SingleAssignmentDisposable();
			var ctr = default(CancellationTokenRegistration);

			if (cancellationToken.CanBeCanceled)
			{
				ctr = cancellationToken.Register(() =>
				{
					completion.TrySetCanceled();
					disposable.Dispose();
				});
			}

			disposable.Disposable = source.ScheduleTask(
				async (ct, _) =>
				{
					try
					{
						var result = await taskBuilder(ct);
						completion.TrySetResult(result);
					}
					catch (Exception e)
					{
						completion.TrySetException(e);
					}
					finally
					{
						ctr.Dispose();
					}
				}
			);

			return completion.Task;
		}

		/// <summary>
		/// Awaits a task execution on the specified scheduler, providing the result.
		/// </summary>
		/// <returns>A task that will provide the result of the execution.</returns>
		public static Task<T> Run<T>(this IScheduler source, Func<T> func, CancellationToken cancellationToken)
		{
			var completion = new FastTaskCompletionSource<T>();

			var disposable = new SingleAssignmentDisposable();
			var ctr = default(CancellationTokenRegistration);

			if (cancellationToken.CanBeCanceled)
			{
				ctr = cancellationToken.Register(() =>
				{
					completion.TrySetCanceled();
					disposable.Dispose();
				});
			}

			disposable.Disposable = source.Schedule(
				() =>
				{
					if (disposable.IsDisposed)
					{
						return; // CT canceled
					}

					try
					{
						var result = func();
						completion.TrySetResult(result);
					}
					catch (Exception e)
					{
						completion.TrySetException(e);
					}
					finally
					{
						ctr.Dispose();
					}
				}
			);

			return completion.Task;
		}

		/// <summary>
		/// Awaits a task on the specified scheduler, without providing a result.
		/// </summary>
		/// <returns>A task that will complete when the work has completed.</returns>
		public static Task Run(this IScheduler source, Func<CancellationToken, Task> taskBuilder, CancellationToken ct)
		{
			return Run(
				source,
				async ct2 => { await taskBuilder(ct2); return Unit.Default; },
				ct
			);
		}

		/// <summary>
		/// Awaits a task on the specified scheduler, without providing a result.
		/// </summary>
		/// <returns>A task that will complete when the work has completed.</returns>
		public static Task Run(this IScheduler source, Action action, CancellationToken ct)
		{
			var completion = new FastTaskCompletionSource<Unit>();

			var disposable = new SingleAssignmentDisposable();
			var ctr = default(CancellationTokenRegistration);

			if (ct.CanBeCanceled)
			{
				ctr = ct.Register(() =>
				{
					completion.TrySetCanceled();
					disposable.Dispose();
				});
			}

			disposable.Disposable = source.Schedule(
				() =>
				{
					if (disposable.IsDisposed)
					{
						return; // CT canceled
					}

					try
					{
						action();
						completion.TrySetResult(Unit.Default);
					}
					catch (Exception e)
					{
						completion.TrySetException(e);
					}
					finally
					{
						ctr.Dispose();
					}
				}
			);

			return completion.Task;
		}

		// WARNING: Do not replace with a call to InvokeTaskWithDisposable with .ContinueWith(_ => Disposable.Empty) because
		// exceptions are not handled correctly by the scheduler. Hence, we need to keep the two methods (InvokeTask & InvokeTaskWithDisposable)
		private static IDisposable InvokeTask<TState>(
			IScheduler scheduler,
			TState state,
			Func<CancellationToken, IScheduler, TState, Task> taskBuilder,
			SchedulerSynchronizationContext schedulerContext)
		{
			var subscriptions = new SerialDisposable();

			var cancellationDisposable = new CancellationDisposable().DisposeWith(subscriptions);

			using (SynchronizationContextHelper.ScopedSet(schedulerContext))
			{
				taskBuilder(cancellationDisposable.Token, scheduler, state)
					.ContinueWith(t =>
					{
						if (t.IsFaulted)
						{
							subscriptions.Disposable = scheduler.Schedule(() => { t.Exception.Handle(e => e is OperationCanceledException); });
						}

						else
						{
							subscriptions.Disposable = null;
						}
					}, TaskContinuationOptions.ExecuteSynchronously);
			}

			return subscriptions;
		}

		private static IDisposable InvokeTaskWithDisposable<TState>(
			IScheduler scheduler,
			TState state,
			Func<CancellationToken, IScheduler, TState, Task<IDisposable>> taskBuilder,
			SchedulerSynchronizationContext schedulerContext)
		{
			var subscriptions = new SerialDisposable();

			var cancellationDisposable = new CancellationDisposable().DisposeWith(subscriptions);

			using (SynchronizationContextHelper.ScopedSet(schedulerContext))
			{
				taskBuilder(cancellationDisposable.Token, scheduler, state)
					.ContinueWith(t =>
					{
						if (t.IsCanceled)
						{
							subscriptions.Disposable = null;
						}

						else if (t.IsFaulted)
						{
							subscriptions.Disposable = scheduler.Schedule(() => { t.Exception.Handle(e => e is OperationCanceledException); });
						}

						else
						{
							subscriptions.Disposable = t.Result;
						}
					}, TaskContinuationOptions.ExecuteSynchronously);
			}

			return subscriptions;
		}
	}
}

