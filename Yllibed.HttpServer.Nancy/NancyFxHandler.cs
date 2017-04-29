using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nancy;
using Nancy.Bootstrapper;
using Yllibed.HttpServer.Handlers;

namespace Yllibed.HttpServer
{
	public class NancyFxHandler : IHttpHandler, IDisposable
	{
		private readonly INancyBootstrapper _nancyBootstrapper;
		private INancyEngine _engine;
		private int _isInitialized;

		public NancyFxHandler(INancyBootstrapper nancyBootstrapper = null)
		{
			_nancyBootstrapper = nancyBootstrapper ?? NancyBootstrapperLocator.Bootstrapper;
		}

		public void Start()
		{
			if (Interlocked.CompareExchange(ref _isInitialized, 1, 0) != 0)
			{
				return; // already initialized
			}

			_nancyBootstrapper.Initialise();

			_engine = _engine ?? _nancyBootstrapper.GetEngine();
		}

		public async Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
		{
			// TODO: Request body for POST requests
			var nancyRequest = new Request(request.Method, request.Url, headers: GetNancyHeaders(request.Headers));

			Start();

			using (var nancyContext = await _engine.HandleRequest(nancyRequest, x => x, ct))
			{
				var response = nancyContext.Response;
				string contentType = response?.ContentType ?? "text/plain";
				var resultCode = (uint?) response?.StatusCode ?? 200;
				var resultText = response?.ReasonPhrase ?? "OK";
				IDictionary<string, ImmutableList<string>> headers = GetResponseHeaders(response);

				var stream = new MemoryStream();
				response.Contents(stream);
				stream.Position = 0; // seek back to beginning

				request.SetResponse(contentType, _ => Task.FromResult(stream as Stream), resultCode, resultText, headers);
			}
		}

		private IDictionary<string, IEnumerable<string>> GetNancyHeaders(ImmutableDictionary<string, ImmutableList<string>> requestHeaders)
		{
			var builder = ImmutableDictionary.CreateBuilder<string, IEnumerable<string>>();
			var convertedHeaders = requestHeaders.Select(kvp => new KeyValuePair<string, IEnumerable<string>>(kvp.Key, kvp.Value));
			builder.AddRange(convertedHeaders);
			return builder.ToImmutable();
		}


		private IDictionary<string, ImmutableList<string>> GetResponseHeaders(Response response)
		{
			var builder = ImmutableDictionary.CreateBuilder<string, ImmutableList<string>>();
			var convertedHeaders = response
				.Headers
				.Select(kvp => new KeyValuePair<string, ImmutableList<string>>(kvp.Key, ImmutableList.Create(kvp.Value)));
			builder.AddRange(convertedHeaders);

			foreach (var cookie in response.Cookies)
			{
				builder["Set-Cookie"] = builder.ContainsKey("Set-Cookie")
					? builder["Set-Cookie"].Add(cookie.ToString())
					: ImmutableList.Create(cookie.ToString());
			}

			return builder.ToImmutable();
		}

		public void Dispose()
		{
			_engine.Dispose();
		}
	}
}
