namespace Yllibed.HttpServer.Tests;

using Yllibed.HttpServer.Handlers;

[TestClass]
public class GuardHandlerFixture : FixtureBase
{
	[TestMethod]
	public async Task GuardHandler_UrlTooLong_Returns414()
	{
		using var sut = new Server();
		// Very small limit for test
		sut.RegisterHandler(new GuardHandler(maxUrlLength: 10));
		sut.RegisterHandler(new StaticHandler("ok", "text/plain", "OK"));

		var (uri4, _) = sut.Start();
		var longPath = new string('a', 50);
		var requestUri = new Uri(uri4, $"{longPath}");

		using var client = new HttpClient();
		var response = await client.GetAsync(requestUri, CT).ConfigureAwait(false);
		response.StatusCode.Should().Be(HttpStatusCode.RequestUriTooLong);
	}

	[TestMethod]
	public async Task GuardHandler_ContentLengthTooLarge_Returns413()
	{
		using var sut = new Server();
		// Limit to 16 bytes
		sut.RegisterHandler(new GuardHandler(maxBodyBytes: 16));
		sut.RegisterHandler(new StaticHandler("ok", "text/plain", "OK"));

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "ok");

		using var client = new HttpClient();
		var content = new string('x', 100); // > 16 bytes
		var response = await client.PostAsync(requestUri, new StringContent(content), CT).ConfigureAwait(false);
		response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
	}

	[TestMethod]
	public async Task GuardHandler_HeadersTooLarge_Returns431()
	{
		using var sut = new Server();
		// Keep overall header size tiny to force 431
		sut.RegisterHandler(new GuardHandler(maxHeadersTotalSize: 100));
		sut.RegisterHandler(new StaticHandler("ok", "text/plain", "OK"));

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "ok");

		using var client = new HttpClient();
		var msg = new HttpRequestMessage(HttpMethod.Get, requestUri);
		msg.Headers.Add("X-Long", new string('z', 1024));
		var response = await client.SendAsync(msg, CT).ConfigureAwait(false);
		response.StatusCode.Should().Be((HttpStatusCode)431);
	}
	[TestMethod]
	public async Task GuardHandler_MethodNotAllowed_Returns405()
	{
		using var sut = new Server();
		sut.RegisterHandler(new GuardHandler(allowedMethods: new[] { "GET" }));
		sut.RegisterHandler(new StaticHandler("/ok", "text/plain", "OK"));

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "/ok");

		using var client = new HttpClient();
		var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, requestUri), CT).ConfigureAwait(false);
		response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
	}

	[TestMethod]
	public async Task GuardHandler_ForbiddenHost_Returns403()
	{
		using var sut = new Server();
		sut.RegisterHandler(new GuardHandler(allowedHosts: new[] { "localhost" }));
		sut.RegisterHandler(new StaticHandler("/ok", "text/plain", "OK"));

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "/ok");

		using var client = new HttpClient();
		var msg = new HttpRequestMessage(HttpMethod.Get, requestUri);
		// Force a different Host header value to trigger 403: use machine name if not localhost
		msg.Headers.Host = "example.com";
		var response = await client.SendAsync(msg, CT).ConfigureAwait(false);
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[TestMethod]
	public async Task GuardHandler_TooManyHeaders_Returns431()
	{
		using var sut = new Server();
		// Set very low header count limit to trigger 431 (Host header alone is 1; we'll add another)
		sut.RegisterHandler(new GuardHandler(maxHeadersCount: 1));
		sut.RegisterHandler(new StaticHandler("/ok", "text/plain", "OK"));

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "/ok");

		using var client = new HttpClient();
		var msg = new HttpRequestMessage(HttpMethod.Get, requestUri);
		msg.Headers.Add("X-One", "1");
		var response = await client.SendAsync(msg, CT).ConfigureAwait(false);
		response.StatusCode.Should().Be((HttpStatusCode)431);
	}

	[TestMethod]
	public async Task GuardHandler_PassesThrough_ToNextHandler_WhenValid()
	{
		using var sut = new Server();
		sut.RegisterHandler(new GuardHandler(allowedMethods: new[] { "GET" }));
		sut.RegisterHandler(new StaticHandler("/ok", "text/plain", "OK"));

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "/ok");

		using var client = new HttpClient();
		var response = await client.GetAsync(requestUri, CT).ConfigureAwait(false);
		response.StatusCode.Should().Be(HttpStatusCode.OK);
  (await response.Content.ReadAsStringAsync(CT).ConfigureAwait(false)).Should().Be("OK");
	}

	[TestMethod]
	public async Task GuardHandler_AsWrapper_CallsInnerOnlyOnPass()
	{
		using var sut = new Server();
		var inner = new StaticHandler("/ok", "text/plain", "OK");
		var guard = new GuardHandler(allowedMethods: new[] { "GET" }, inner: inner);
		sut.RegisterHandler(guard);

		var (uri4, _) = sut.Start();
		var requestUri = new Uri(uri4, "/ok");

		using var client = new HttpClient();
		// DELETE should be blocked by protection and inner should not run
		var blocked = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, requestUri), CT).ConfigureAwait(false);
		blocked.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

		// GET should pass and inner returns OK
		var allowed = await client.GetAsync(requestUri, CT).ConfigureAwait(false);
		allowed.StatusCode.Should().Be(HttpStatusCode.OK);
	}
}
