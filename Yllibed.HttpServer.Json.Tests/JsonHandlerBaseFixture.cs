using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Yllibed.HttpServer.Json;

namespace Yllibed.HttpServer.Json.Tests;

[TestClass]
public class JsonHandlerBaseFixture : FixtureBase
{
	[TestMethod]
	public async Task TestSimpleGet_WithJsonHandler()
	{
		var server = new Server();

		var (uri4, uri6) = server.Start();
		var requestUri = uri4 + "abcd?a=1&b=%20%2020";

		var sut = new MyJsonHandler("GET", "abcd");

		using (server.RegisterHandler(sut))
		{
			using (var client = new HttpClient())
			{
				var response = await client.GetAsync(requestUri, CT);
				response.StatusCode.Should().Be(HttpStatusCode.OK);

				var result = JsonConvert.DeserializeObject<MyResultPayload>(await response.Content.ReadAsStringAsync(CT).ConfigureAwait(false));
				result.Should().NotBeNull();
				result.Should().BeEquivalentTo(new { A = "1", B = "  20" });
			}
		}
	}

	public class MyResultPayload
	{
		public string? A { get; set; }
		public string? B { get; set; }
	}

	private sealed class MyJsonHandler : JsonHandlerBase<MyResultPayload>
	{
		public MyJsonHandler(string method, string path) : base(method, path)
		{
		}

		protected override async Task<(MyResultPayload result, ushort statusCode)> ProcessRequest(CancellationToken ct, string relativePath, IDictionary<string, string[]> queryParameters)
		{
			queryParameters.TryGetValue("a", out var a);
			queryParameters.TryGetValue("b", out var b);

			await Task.Yield();

			var result = new MyResultPayload
			{
				A = a?.FirstOrDefault(),
				B = b?.FirstOrDefault(),
			};

			return (result, 200);
		}
	}
}
