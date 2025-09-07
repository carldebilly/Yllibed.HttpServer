namespace Yllibed.Handlers.Uno;

public interface IAuthCallbackHandler : IHttpHandler
{
	public Uri CallbackUri { get; }
}
