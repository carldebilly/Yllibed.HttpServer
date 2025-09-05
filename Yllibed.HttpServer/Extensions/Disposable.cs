using System;

namespace Yllibed.HttpServer.Extensions;

internal static class Disposable
{
	private sealed class DisposableAction : IDisposable
	{
		private readonly Action _disposeAction;

		private bool _disposed;

		public DisposableAction(Action disposeAction) => _disposeAction = disposeAction;

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_disposeAction();
		}
	}

	public static IDisposable Create(Action disposeAction) => new DisposableAction(disposeAction);


	private sealed class DisposableAction<T> : IDisposable
	{
		private readonly Action<T> _disposeAction;
		private readonly T _state;

		private bool _disposed;

		public DisposableAction(Action<T> disposeAction, T state)
		{
			_disposeAction = disposeAction;
			_state = state;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			_disposeAction(_state);
		}
	}

	public static IDisposable Create<T>(T state, Action<T> disposeAction) => new DisposableAction<T>(disposeAction, state);
}
