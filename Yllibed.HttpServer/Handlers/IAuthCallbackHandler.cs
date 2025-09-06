namespace Yllibed.HttpServer.Handlers;

public interface IAuthCallbackHandler : IHttpHandler
{
	public Uri CallbackUri { get; }
}
