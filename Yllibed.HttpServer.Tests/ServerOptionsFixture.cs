namespace Yllibed.HttpServer.Tests;

[TestClass]
public class ServerOptionsFixture
{
	[TestMethod]
	public void ServerOptions_InitValues_AreUsedByServer()
	{
		var options = new ServerOptions
		{
			Port = 0,
			BindAddress4 = IPAddress.Loopback,
			BindAddress6 = IPAddress.IPv6Loopback,
			Hostname4 = "127.0.0.5",
			Hostname6 = "::5"
		};

		using var sut = new Server(options);
		var (uri4, uri6) = sut.Start();

		// Hostnames should be used in the composed URIs
		uri4.Host.Should().Be("127.0.0.5");
		// IPv6 host in Uri.Host returns without brackets, but ToString/AbsoluteUri contains brackets
		uri6.Host.Trim('[', ']').Should().Be("::5");
		uri6.AbsoluteUri.Should().Contain("[::5]");
	}

	[TestMethod]
	public async Task ServerOptions_CustomHostnames_CanAcceptConnections()
	{
		var options = new ServerOptions
		{
			Port = 0,
			BindAddress4 = IPAddress.Loopback,
			BindAddress6 = IPAddress.IPv6Loopback,
			Hostname4 = "127.0.0.1",
			Hostname6 = "::1"
		};

		using var sut = new Server(options);
		var (uri4, uri6) = sut.Start();

		using var client = new HttpClient();
		var response4 = await client.GetAsync(uri4).ConfigureAwait(false);
		response4.StatusCode.Should().Be(HttpStatusCode.NotFound);

		using var client6 = new HttpClient();
		var response6 = await client6.GetAsync(uri6).ConfigureAwait(false);
		response6.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
