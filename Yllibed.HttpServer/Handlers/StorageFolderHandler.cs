#if false
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yllibed.HttpServer.Handlers
{
	/// <summary>
	/// This handler will serve the files on disk to a relative url.
	/// </summary>
	/// <remarks>
	/// No "directory browsing" feature.
	/// WARNING: No protection against parent folder access.
	/// </remarks>
	public class StorageFolderHandler : IHttpHandler
	{
		private readonly IStorageFolder _rootFolder;
		private readonly string _relativePath;
		private readonly bool _generate404OnNotFound;

		public StorageFolderHandler(IStorageFolder rootFolder, string relativePath, bool generate404OnNotFound = true)
		{
			_rootFolder = rootFolder;
			_relativePath = relativePath;
			_generate404OnNotFound = generate404OnNotFound;
		}

		public async Task HandleRequest(CancellationToken ct, Uri serverRoot, string relativePath, IHttpServerRequest request)
		{
			if (relativePath.StartsWith(_relativePath, StringComparison.OrdinalIgnoreCase))
			{
				var p = serverRoot
					.MakeRelativeUri(request.Url)
					.ToString()
					.Replace('/', '\\');

				StorageFile file;
				try
				{
					file = await _rootFolder.GetFileAsync(p).AsTask(ct);
				}
				catch
				{
					if (_generate404OnNotFound)
					{
						request.SetResponse("text/plain", token => Create404(token, request.Url), 404, "NOT FOUND");
					}

					return;
				}

				if (file.IsAvailable)
				{
					request.SetResponse(file.ContentType, ct2 => file.OpenStreamForReadAsync());
				}
			}
		}

#pragma warning disable 1998
		private Task<Stream> Create404(CancellationToken ct, Uri url)
		{
			var stream = new MemoryStream(Encoding.UTF8.GetBytes("Unable to find resource at url " + url));
			return Task.FromResult(stream as Stream);
		}
#pragma warning restore 1998
	}
}
#endif