#pragma warning disable 1998

namespace Yllibed.HttpServer.Handlers;

public class StaticHandler : IHttpHandler
{
	private readonly string _path;
	private readonly string _responseContentType;
	private readonly byte[] _responseBody;

	public StaticHandler(string path, string responseContentType, string responseBody)
	{
		_path = path;
		_responseContentType = responseContentType;
		_responseBody = Encoding.UTF8.GetBytes(responseBody);

		if (!_path.StartsWith("/", StringComparison.Ordinal))
		{
			_path = "/" + _path;
		}
	}

	public async Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
	{
		if (relativePath.Equals(_path, StringComparison.OrdinalIgnoreCase))
		{
			if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
			{
				request.SetResponse(_responseContentType, GetStream);
				return;
			}
			request.SetResponse("text/plain", "Method not authorized - use a GET", 405, "METHOD NOT ALLOWED");
		}
	}

	private Task<Stream> GetStream(CancellationToken ct) => Task.FromResult<Stream>(new MemoryStream(_responseBody));
}
