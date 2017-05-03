using System;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yllibed.HttpServer.Handlers;

namespace Yllibed.HttpServer.Tests
{
	[TestClass]
	public class HttpServerFixture : FixtureBase
	{
		[TestMethod]
		public async Task HttpServer_Test404WhenNoHandlers()
		{
			using (var scheduler = new EventLoopScheduler())
			{
				var sut = new HttpServer(scheduler: scheduler);

				var serverUri = await sut.GetRootUri(_ct);
				var requestUri = serverUri;

				using (var client = new HttpClient())
				{
					var response = await client.GetAsync(requestUri, _ct);
					response.StatusCode.Should().Be(HttpStatusCode.NotFound);
				}
			}
		}

		[TestMethod]
		public async Task HttpServer_TestWithStaticHandler()
		{
			using (var scheduler = new EventLoopScheduler())
			{
				var sut = new HttpServer(scheduler: scheduler);
				sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

				var serverUri = await sut.GetRootUri(_ct);
				var requestUri = new Uri(serverUri, "abcd");

				using (var client = new HttpClient())
				{
					var response = await client.GetAsync(requestUri, _ct);
					response.StatusCode.Should().Be(HttpStatusCode.OK);
					(await response.Content.ReadAsStringAsync()).Should().Be("1234");
				}
			}
		}

		[TestMethod]
		public async Task HttpServer_TestWithStaticHandler_WithPost_ShouldReturn404()
		{
			using (var scheduler = new EventLoopScheduler())
			{
				var sut = new HttpServer(scheduler: scheduler);
				sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

				var serverUri = await sut.GetRootUri(_ct);
				var requestUri = new Uri(serverUri, "abcd");

				using (var client = new HttpClient())
				{
					var response = await client.PostAsync(requestUri, null, _ct);
					response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
				}
			}
		}

		[TestMethod]
		public async Task HttpServer_TestWithStaticHandlerOnRoot()
		{
			using (var scheduler = new EventLoopScheduler())
			{
				var sut = new HttpServer(scheduler: scheduler);
				sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

				var serverUri = await sut.GetRootUri(_ct);
				var requestUri = new Uri(serverUri, "abcd");

				using (var client = new HttpClient())
				{
					var response = await client.GetAsync(requestUri, _ct);
					response.StatusCode.Should().Be(HttpStatusCode.OK);
					(await response.Content.ReadAsStringAsync()).Should().Be("1234");
				}
			}
		}
	}
}