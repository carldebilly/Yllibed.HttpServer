using System.Net.Http;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nancy;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Yllibed.HttpServer.Nancy.Tests
{
	[TestClass]
	public class NancyFxHandlerFixture : FixtureBase
	{
		[TestMethod]
		public async Task TestSimpleGet_WithNancyEngine()
		{
			using (var scheduler = new EventLoopScheduler())
			{
				var server = new HttpServer(scheduler: scheduler);

				var serverUri = await server.GetRootUri(_ct);
				var requestUri = serverUri;

				using (var sut = new NancyFxHandler())
				using (server.RegisterHandler(sut))
				{
					using (var client = new HttpClient())
					{
						var response = await client.GetAsync(requestUri, _ct);
						response.StatusCode.Should().Be(HttpStatusCode.OK);
						var content = await response.Content.ReadAsStringAsync();
						content.ShouldBeEquivalentTo("It Works");
					}
				}
			}
		}
	}

	public class TestModule : NancyModule
	{
		public TestModule()
		{
			Get("/", ctx => "It Works");
		}
	}
}