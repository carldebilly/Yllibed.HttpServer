using System;
using Yllibed.HttpServer.Helpers;
#pragma warning disable MA0001

namespace Yllibed.HttpServer.Extensions;

public static class HttpServerRequestAcceptExtensions
{
	/// <summary>
	/// Validates if the request's Accept header allows the specified media type.
	/// </summary>
	public static bool ValidateAccept(this IHttpServerRequest request, string mediaType)
		=> AcceptHeaderHelper.IsAccepted(request.Accept, mediaType);
}
