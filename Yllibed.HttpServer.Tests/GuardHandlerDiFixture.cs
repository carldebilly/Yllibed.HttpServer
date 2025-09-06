namespace Yllibed.HttpServer.Tests;

using Microsoft.Extensions.DependencyInjection;
using Yllibed.HttpServer.Extensions;
using Yllibed.HttpServer.Handlers;

[TestClass]
public class GuardHandlerDiFixture : FixtureBase
{
	[TestMethod]
	public async Task AddGuardHandlerAndRegister_RegistersIntoServerPipeline()
	{
		var services = new ServiceCollection();
		services.AddYllibedHttpServer();
		services.AddGuardHandlerAndRegister(opts =>
		{
			opts.AllowedMethods = new[] { "GET" };
		});

		using var sp = services.BuildServiceProvider();
		var server = sp.GetRequiredService<Server>();
		server.RegisterHandler(new StaticHandler("/ok", "text/plain", "OK"));
		var (uri4, _) = server.Start();

		var requestUri = new Uri(uri4, "/ok");

		using var client = new HttpClient();
		// DELETE should be blocked by protection without manually resolving the handler
		var blocked = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, requestUri), CT).ConfigureAwait(false);
		blocked.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

		// GET passes
		var allowed = await client.GetAsync(requestUri, CT).ConfigureAwait(false);
		allowed.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
