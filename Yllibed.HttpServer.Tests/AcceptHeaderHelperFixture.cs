using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yllibed.HttpServer;
using Yllibed.HttpServer.Extensions;

namespace Yllibed.HttpServer.Tests;

[TestClass]
public class AcceptHeaderHelperFixture
{
	private static bool IsAccepted(string? accept, string mediaType) => new FakeRequest(accept).ValidateAccept(mediaType);

	private sealed class FakeRequest : IHttpServerRequest
	{
		public FakeRequest(string? accept) { Accept = accept; }
		public string Method => "GET";
		public string Path => "/";
		public string? Http => "HTTP/1.1";
		public string? Host => "localhost";
		public string? HostName => "localhost";
		public int Port => 80;
		public string? Referer => null;
		public string? UserAgent => "UnitTest";
		public Uri Url => new Uri("http://localhost/");
		public int? ContentLength => null;
		public string? ContentType => null;
		public string? Body => null;
		public string? Accept { get; }
		public IReadOnlyDictionary<string, IReadOnlyCollection<string>>? Headers => null;
  public void SetResponse(string contentType, Func<CancellationToken, Task<Stream>> streamFactory, uint resultCode = 200, string resultText = "OK", IReadOnlyDictionary<string, IReadOnlyCollection<string>>? headers = null) => throw new NotSupportedException();
		public void SetResponse(string contentType, string content, uint resultCode = 200, string resultText = "OK", IReadOnlyDictionary<string, IReadOnlyCollection<string>>? headers = null) => throw new NotSupportedException();
		public void SetStreamingResponse(string contentType, Func<TextWriter, CancellationToken, Task> writer, uint resultCode = 200, string resultText = "OK", IReadOnlyDictionary<string, IReadOnlyCollection<string>>? headers = null) => throw new NotSupportedException();
	}

	[DataTestMethod]
	// No Accept header means no constraint
	[DataRow(null, "text/html", true)]
	[DataRow("", "text/html", true)]
	// Wildcard */*
	[DataRow("*/*", "text/html", true)]
	[DataRow("*/*;q=1", "text/html", true)]
	[DataRow("*/*;q=0", "text/html", false)]
	// Type wildcard
	[DataRow("text/*", "text/html", true)]
	[DataRow("text/*;q=0", "text/html", false)]
	[DataRow("application/*", "text/html", false)]
	[DataRow("application/*, text/*;q=0", "text/html", false)]
	// Exact matches (case-insensitive)
	[DataRow("text/html", "text/html", true)]
	[DataRow("text/HTML", "text/html", true)]
	[DataRow("TEXT/HTML", "text/html", true)]
	[DataRow("text/html;q=0", "text/html", false)]
	[DataRow("text/html;level=1", "text/html", true)]
	[DataRow("text/html;q=abc", "text/html", true)]
	// Multiple entries and precedence
	[DataRow("text/*;q=0, text/html;q=0.8", "text/html", true)]
	[DataRow("text/*;q=0, */*;q=0", "text/html", false)]
	[DataRow("application/json;q=0, text/html", "text/html", true)]
	[DataRow("application/json;q=0, text/html;q=0", "text/html", false)]
	[DataRow("application/json;q=0, text/*;q=0, */*;q=1", "text/html", true)]
	// Malformed/edge tokens
	[DataRow("texthtml", "text/html", false)] // no slash, not exact match
	[DataRow(",,, text/html", "text/html", true)]
	[DataRow("text/*; q=  0.5", "text/plain", true)]
	[DataRow("text/* ; q = 0 ", "text/plain", false)]
	// Order independence
	[DataRow("text/html;q=0, */*;q=1", "text/html", true)]
	[DataRow("*/*;q=0, text/html;q=1", "text/html", true)]
	public void IsAccepted_VariousCases_ShouldMatchExpectation(string? accept, string mediaType, bool expected)
	{
		var result = IsAccepted(accept, mediaType);
		result.Should().Be(expected, $"Accept='{accept}' should{(expected ? string.Empty : " not")} accept '{mediaType}'");
	}
}
