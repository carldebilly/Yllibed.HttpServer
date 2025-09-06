#nullable disable
using System.Diagnostics;

namespace Yllibed.HttpServer.Tests;

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
	public void Terminate() => _ctSource.Dispose();
}
