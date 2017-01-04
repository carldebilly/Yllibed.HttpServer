using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Yllibed.Framework.Extensions
{
	public static class DisposableExtensions
	{
		public static T DisposeWith<T>(this T disposable, ICollection<IDisposable> collection) where T : IDisposable
		{
			collection.Add(disposable);
			return disposable;
		}

		public static T DisposeWith<T>(this T disposable, SerialDisposable serialDisposable) where T : IDisposable
		{
			serialDisposable.Disposable = disposable;
			return disposable;
		}

		public static T DisposeWith<T>(this T disposable, SingleAssignmentDisposable singleAssignmentDisposable) where T : IDisposable
		{
			singleAssignmentDisposable.Disposable = disposable;
			return disposable;
		}

		public static IObservable<T> DisposeWith<T>(this IConnectableObservable<T> connectableObservable, ICollection<IDisposable> collection)
		{
			collection.Add(connectableObservable.Connect());
			return connectableObservable;
		}

		public static IObservable<T> DisposeWith<T>(this IConnectableObservable<T> connectableObservable, SerialDisposable serialDisposable)
		{
			serialDisposable.Disposable = connectableObservable.Connect();
			return connectableObservable;
		}

		public static IObservable<T> DisposeWith<T>(this IConnectableObservable<T> connectableObservable, SingleAssignmentDisposable singleAssignmentDisposable)
		{
			singleAssignmentDisposable.Disposable = connectableObservable.Connect();
			return connectableObservable;
		}
	}
}
