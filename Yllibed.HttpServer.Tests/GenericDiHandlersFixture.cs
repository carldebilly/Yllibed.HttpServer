namespace Yllibed.HttpServer.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yllibed.HttpServer.Extensions;
using Yllibed.HttpServer.Handlers;

[TestClass]
public class GenericDiHandlersFixture : FixtureBase
{
	private sealed class EchoOptions
	{
		public string? Message { get; set; }
	}

	private sealed class EchoHandler : IHttpHandler
	{
		private readonly string _message;
		public EchoHandler(IOptions<EchoOptions> options)
		{
			_message = options.Value.Message ?? "(null)";
		}

		public Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
		{
			if (string.Equals(relativePath, "/echo", StringComparison.OrdinalIgnoreCase))
			{
				request.SetResponse("text/plain", _message);
			}
			return Task.CompletedTask;
		}
	}

	[TestMethod]
	public async Task AddHttpHandlerAndRegister_RegistersSimpleHandler()
	{
		var services = new ServiceCollection();
		services.AddYllibedHttpServer(_ => { });

		// Register a simple preconfigured StaticHandler via DI factory and ensure generic method can still wire it:
		services.AddSingleton(new StaticHandler("/ping", "text/plain", "pong"));
		services.AddHttpHandlerAndRegister<StaticHandler>(); // Remove the duplicate call

		using var sp = services.BuildServiceProvider();
		var server = sp.GetRequiredService<Server>();
		var (uri4, _) = server.Start();

		using var client = new HttpClient();
		var res = await client.GetAsync(new Uri(uri4, "/ping"), CT).ConfigureAwait(false);
		res.StatusCode.Should().Be(HttpStatusCode.OK);
		(await res.Content.ReadAsStringAsync(CT).ConfigureAwait(false)).Should().Be("pong");
	}

	[TestMethod]
	public async Task AddHttpHandlerAndRegister_WithOptions_ConfiguresAndRegisters()
	{
		var services = new ServiceCollection();
		services.AddYllibedHttpServer(_ => { });
		services.AddHttpHandlerAndRegister<EchoHandler, EchoOptions>(o => o.Message = "hello from options");

		using var sp = services.BuildServiceProvider();
		var server = sp.GetRequiredService<Server>();
		var (uri4, _) = server.Start();

		using var client = new HttpClient();
		var res = await client.GetAsync(new Uri(uri4, "/echo"), CT).ConfigureAwait(false);
		res.StatusCode.Should().Be(HttpStatusCode.OK);
		(await res.Content.ReadAsStringAsync(CT).ConfigureAwait(false)).Should().Be("hello from options");
	}
}
