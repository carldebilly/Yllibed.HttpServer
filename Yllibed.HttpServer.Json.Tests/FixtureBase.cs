#nullable disable
using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yllibed.HttpServer.Json.Tests
{
	public class FixtureBase
	{
		protected CancellationToken CT;
		private CancellationTokenSource _ctSource;

		[TestInitialize]
		public void Initialize()
		{
			_ctSource = Debugger.IsAttached
				? new CancellationTokenSource()
				: new CancellationTokenSource(TimeSpan.FromSeconds(10));
			CT = _ctSource.Token;
		}

		[TestCleanup]
		public void Terminate()
		{
			_ctSource.Dispose();
		}
	}
}
