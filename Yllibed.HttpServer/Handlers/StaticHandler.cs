using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.HttpServer.Handlers
{
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

			if (!_path.StartsWith("/"))
			{
				_path = "/" + _path;
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public async Task HandleRequest(CancellationToken ct, Uri serverRoot, string relativePath, IHttpServerRequest request)
		{
			if (relativePath.Equals(_path, StringComparison.OrdinalIgnoreCase))
			{
				request.SetResponse(_responseContentType, GetStream);
			}
		}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

		private Task<Stream> GetStream(CancellationToken ct)
		{
			return Task.FromResult<Stream>(new MemoryStream(_responseBody));
		}
	}
}
