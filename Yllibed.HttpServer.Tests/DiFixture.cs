using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yllibed.HttpServer.Extensions;

namespace Yllibed.HttpServer.Tests;

[TestClass]
public class DiFixture : FixtureBase
{
	[TestMethod]
	public async Task Server_CanBeResolvedFromDI_WithConfigureOptions()
	{
		var services = new ServiceCollection();

		// Use Configure<> like in the README example
		services.Configure<ServerOptions>(opts =>
		{
			opts.Port = 0; // Use dynamic port for test
			opts.Hostname4 = "127.0.0.1";
			opts.Hostname6 = "::1";
			opts.BindAddress4 = System.Net.IPAddress.Loopback;
			opts.BindAddress6 = System.Net.IPAddress.IPv6Loopback;
		});

		// Register Server explicitly via factory to avoid ambiguous constructor selection
		services.AddSingleton<Server>(sp => new Server(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServerOptions>>()));

		var sp = services.BuildServiceProvider();

		var server = sp.GetRequiredService<Server>();
		using (server)
		{
			var (uri4, uri6) = server.Start();

			using var client = new HttpClient();
			var response = await client.GetAsync(uri4, CT).ConfigureAwait(false);
			response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
		}
	}

	[TestMethod]
	public async Task Server_CanBeResolvedFromDI_WithActivatorUtilitiesConstructor()
	{
		var services = new ServiceCollection();

		services.Configure<ServerOptions>(opts =>
		{
			opts.Port = 0;
			opts.Hostname4 = "127.0.0.1";
			opts.Hostname6 = "::1";
			opts.BindAddress4 = System.Net.IPAddress.Loopback;
			opts.BindAddress6 = System.Net.IPAddress.IPv6Loopback;
		});

		// This should now work without explicit factory thanks to [ActivatorUtilitiesConstructor]
		services.AddSingleton<Server>();

		var sp = services.BuildServiceProvider();

		var server = sp.GetRequiredService<Server>();
		using (server)
		{
			var (uri4, uri6) = server.Start();

			using var client = new HttpClient();
			var response = await client.GetAsync(uri4, CT).ConfigureAwait(false);
			response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
		}
	}

	[TestMethod]
	public async Task Server_CanBeRegisteredWithExtensionMethod()
	{
		var services = new ServiceCollection();

		// Using the extension method - cleanest approach
		services.AddYllibedHttpServer(opts =>
		{
			opts.Port = 0;
			opts.Hostname4 = "127.0.0.1";
			opts.Hostname6 = "::1";
			opts.BindAddress4 = System.Net.IPAddress.Loopback;
			opts.BindAddress6 = System.Net.IPAddress.IPv6Loopback;
		});

		var sp = services.BuildServiceProvider();

		var server = sp.GetRequiredService<Server>();
		using (server)
		{
			var (uri4, uri6) = server.Start();

			using var client = new HttpClient();
			var response = await client.GetAsync(uri4, CT).ConfigureAwait(false);
			response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
		}
	}

	[TestMethod]
	public async Task README_Example_Works()
	{
		// Test the exact example from README but with dynamic port
		var services = new ServiceCollection();
		services.Configure<ServerOptions>(opts =>
		{
			opts.Port = 0; // Dynamic port for test instead of 8080
			opts.Hostname4 = "127.0.0.1";
			opts.Hostname6 = "::1";
		});
		services.AddSingleton<Server>(sp => new Server(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ServerOptions>>()));

		var sp = services.BuildServiceProvider();
		var server = sp.GetRequiredService<Server>();
		server.RegisterHandler(new Yllibed.HttpServer.Handlers.StaticHandler("/", "text/plain", "Hello, world!"));

		using (server)
		{
			var (uri4, uri6) = server.Start();

			using var client = new HttpClient();
			var response = await client.GetAsync(uri4, CT).ConfigureAwait(false);
			response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

			var content = await response.Content.ReadAsStringAsync(CT).ConfigureAwait(false);
			content.Should().Be("Hello, world!");
		}
	}
}
