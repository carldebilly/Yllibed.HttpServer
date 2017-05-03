using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Yllibed.HttpServer.Json.Tests
{
	[TestClass]
	public class JsonHandlerBaseFixture : FixtureBase
	{
		[TestMethod]
		public async Task TestSimpleGet_WithNancyEngine()
		{
			using (var scheduler = new EventLoopScheduler())
			{
				var server = new HttpServer(scheduler: scheduler);

				var serverUri = await server.GetRootUri(_ct);
				var requestUri = serverUri + "abcd?a=1&b=%20%2020";

				var sut = new MyJsonHandler("GET", "abcd");

				using (server.RegisterHandler(sut))
				{
					using (var client = new HttpClient())
					{
						var response = await client.GetAsync(requestUri, _ct);
						response.StatusCode.Should().Be(HttpStatusCode.OK);

						var result = JsonConvert.DeserializeObject<MyResultPayload>(await response.Content.ReadAsStringAsync());
						result.Should().NotBeNull();
						result.A.ShouldBeEquivalentTo("1");
						result.B.ShouldBeEquivalentTo("  20");
					}
				}
			}
		}

		public class MyResultPayload
		{
			public string A { get; set; }
			public string B { get; set; }
		}

		internal class MyJsonHandler : JsonHandlerBase<MyResultPayload>
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
					B = b?.FirstOrDefault()
				};

				return (result, 200);
			}
		}
	}
}
