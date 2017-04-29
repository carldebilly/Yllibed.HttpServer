using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yllibed.HttpServer.Nancy.Tests
{
	public class FixtureBase
	{
		private CancellationTokenSource _ctSource;
		protected CancellationToken _ct;

		[TestInitialize]
		public void Initialize()
		{
			_ctSource = Debugger.IsAttached ? new CancellationTokenSource() : new CancellationTokenSource(TimeSpan.FromSeconds(10));
			_ct = _ctSource.Token;
		}

		[TestCleanup]
		public void Terminate()
		{
			_ctSource.Dispose();
		}
	}
}
