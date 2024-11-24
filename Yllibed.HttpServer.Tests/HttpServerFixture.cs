using System;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Http;
using System.Threading;
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
		public async Task HttpServer_Ipv4_Test404WhenNoHandlers()
		{
			using var sut = new Server();

			var (uri4, uri6) = sut.Start();
			var requestUri = uri4;

			using var client = new HttpClient();
			var response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		}

		[TestMethod]
		public async Task HttpServer_Ipv6_Test404WhenNoHandlers()
		{
			using var sut = new Server();

			var (uri4, uri6) = sut.Start();
			var requestUri = uri6;

			using var client = new HttpClient();
			var response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		}

		[TestMethod]
		public async Task HttpServer_TestWithStaticHandler()
		{
			using var sut = new Server();
			sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd");

			using var client = new HttpClient();
			var response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var responseContent = await response.Content.ReadAsStringAsync(CT);
			responseContent.Should().Be("1234");
		}

		[TestMethod]
		public async Task HttpServer_TestWithStaticHandler_WithPost_ShouldReturn404()
		{
			using var sut = new Server();
			sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd");

			using var client = new HttpClient();
			var response = await client.PostAsync(requestUri, new StringContent(string.Empty), CT);
			response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
		}

		[TestMethod]
		public async Task HttpServer_TestWithStaticHandlerOnRoot()
		{
			using var sut = new Server();
			sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd");

			using var client = new HttpClient();
			var response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var responseContent = await response.Content.ReadAsStringAsync(CT);
			responseContent.Should().Be("1234");
		}

		// Load tests
		[TestMethod]
		public async Task HttpServer_TestWithStaticHandlerOnRoot_WithMultipleRequests()
		{
			using var sut = new Server();
			sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd");

			using var client = new HttpClient();
			var tasks = new Task[100];
			for (var i = 0; i < tasks.Length; i++)
			{
				tasks[i] = client.GetAsync(requestUri, CT);
			}

			await Task.WhenAll(tasks);
		}

		private sealed class RandomlySlowHandler : IHttpHandler
		{
			private static readonly Random Rnd = new();

			public async Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
			{
				await Task.Delay(Rnd.Next(1, 100), ct).ConfigureAwait(false);

				if (request.Method.Equals("POST", StringComparison.Ordinal))
				{
					var requestBody = request.Body ?? "EMPTY";
					request.SetResponse(request.ContentType ?? "text/plain", requestBody);
					return;
				}

				request.SetResponse("text/plain", "1234");
			}
		}

		[TestMethod]
		[DataRow(2)]
		[DataRow(20)]
		[DataRow(200)]
		[DataRow(2000)]
		public async Task HttpServer_TestWithStaticHandlerOnRoot_WithMultipleGetRequests_WithDelay(int count)
		{
			using var sut = new Server();
			sut.RegisterHandler(new RandomlySlowHandler());

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd");

			using var client = new HttpClient();
			var tasks = new Task[count];
			for (var i = 0; i < tasks.Length; i++)
			{
				tasks[i] = client.GetAsync(requestUri, CT);
			}

			await Task.WhenAll(tasks);
		}

		[TestMethod]
		[DataRow(2)]
		[DataRow(20)]
		[DataRow(200)]
		[DataRow(2000)]
		public async Task HttpServer_TestWithStaticHandlerOnRoot_WithMultiplePostRequests_WithDelay(int count)
		{
			using var sut = new Server();
			sut.RegisterHandler(new RandomlySlowHandler());

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd");

			using (var client = new HttpClient())
			{
				var tasks = new Task[count];
				for (var i = 0; i < tasks.Length; i++)
				{
					tasks[i] = PostRequest(i);
				}

				async Task PostRequest(int i)
				{
					var content = $"Content {i:x8}";
					var response = await client
						.PostAsync(requestUri, new StringContent(content), CT)
						.ConfigureAwait(false);
					response.StatusCode.Should().Be(HttpStatusCode.OK);
					var responseContent = await response.Content.ReadAsStringAsync(CT).ConfigureAwait(false);
					responseContent.Should().Be(content);
				}

				await Task.WhenAll(tasks);
			}
		}

		[TestMethod]
		public async Task HttpServer_TestWithRelativePathHandler()
		{
			using var sut = new Server();
			var relativePathHandler = new RelativePathHandler("abcd");
			relativePathHandler.RegisterHandler(new StaticHandler("efgh", "text/plain", "1234"));
			sut.RegisterHandler(relativePathHandler);

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd/efgh");

			using var client = new HttpClient();
			var response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var responseContent = await response.Content.ReadAsStringAsync(CT);
			responseContent.Should().Be("1234");
		}

		[TestMethod]
		public async Task HttpServer_TestWithRelativePathHandler_MultiLevel()
		{
			using var sut = new Server();
			var rootPath = new RelativePathHandler("abcd");
			rootPath.RegisterHandler(new StaticHandler("efgh", "text/plain", "1234"));
			sut.RegisterHandler(rootPath);

			var subPath1 = new RelativePathHandler("efgh");
			subPath1.RegisterHandler(new StaticHandler("ijkl", "text/plain", "5678"));
			rootPath.RegisterHandler(subPath1);

			var (uri4, uri6) = sut.Start();

			await Check(new Uri(uri4, "abcd/efgh"), "1234");
			await Check(new Uri(uri4, "abcd/efgh/ijkl"), "5678");

			async Task Check(Uri uri, string expected)
			{
				using var client = new HttpClient();
				var response = await client.GetAsync(uri, CT).ConfigureAwait(false);
				response.StatusCode.Should().Be(HttpStatusCode.OK);
				var responseContent = await response.Content.ReadAsStringAsync(CT).ConfigureAwait(false);
				responseContent.Should().Be(expected);
			}
		}

		[TestMethod]
		public async Task HttpServer_TestWithRelativePathHandler_RegisterAndUnregister()
		{
			using var sut = new Server();
			var relativePathHandler = new RelativePathHandler("abcd");
			relativePathHandler.RegisterHandler(new StaticHandler("efgh", "text/plain", "1234"));
			var registration = sut.RegisterHandler(relativePathHandler);

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd/efgh");

			using var client = new HttpClient();
			var response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var responseContent = await response.Content.ReadAsStringAsync(CT);
			responseContent.Should().Be("1234");

			registration.Dispose();

			response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		}

		[TestMethod]
		public async Task HttpServer_TestWithStaticHandler_RegisterAndUnregister()
		{
			using var sut = new Server();
			var registration = sut.RegisterHandler(new StaticHandler("abcd", "text/plain", "1234"));

			var (uri4, uri6) = sut.Start();
			var requestUri = new Uri(uri4, "abcd");

			using var client = new HttpClient();
			var response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
			var responseContent = await response.Content.ReadAsStringAsync(CT);
			responseContent.Should().Be("1234");

			registration.Dispose();

			response = await client.GetAsync(requestUri, CT);
			response.StatusCode.Should().Be(HttpStatusCode.NotFound);
		}
	}
}
