using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Yllibed.HttpServer.Json.Tests;

[TestClass]
public class JsonHandlerBaseMoreFixture : FixtureBase
{
	private sealed class ThrowingHandler : JsonHandlerBase<object>
	{
		public ThrowingHandler(string method, string path) : base(method, path) { }

		protected override Task<(object result, ushort statusCode)> ProcessRequest(CancellationToken ct, string relativePath, IDictionary<string, string[]> queryParameters)
		{
			throw new InvalidOperationException("boom");
		}
	}

	private sealed class EchoHandler : JsonHandlerBase<object>
	{
		public EchoHandler(string method, string path) : base(method, path) { }

		protected override Task<(object result, ushort statusCode)> ProcessRequest(CancellationToken ct, string relativePath, IDictionary<string, string[]> queryParameters)
		{
			return Task.FromResult<(object, ushort)>((new { ok = true }, 200));
		}
	}

	[TestMethod]
	public async Task JsonHandler_WhenProcessThrows_ShouldReturn500WithPlainText()
	{
		using var server = new Server();
		server.RegisterHandler(new ThrowingHandler("GET", "/err"));
		var (uri4, _) = server.Start();
		var requestUri = new Uri(uri4, "err");

		using var client = new HttpClient();
		var response = await client.GetAsync(requestUri, CT);
		response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
		response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
		var content = await response.Content.ReadAsStringAsync(CT);
		content.Should().Contain("Error processing request");
	}

	[TestMethod]
	public async Task JsonHandler_WrongMethod_ShouldNotHandle_AndServerReturns404()
	{
		using var server = new Server();
		server.RegisterHandler(new EchoHandler("POST", "/j"));
		var (uri4, _) = server.Start();
		var requestUri = new Uri(uri4, "j");

		using var client = new HttpClient();
		var response = await client.GetAsync(requestUri, CT);
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}
}
