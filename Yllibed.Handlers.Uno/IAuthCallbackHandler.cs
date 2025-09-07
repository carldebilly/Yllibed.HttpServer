namespace Yllibed.Handlers.Uno;

public interface IAuthCallbackHandler : IHttpHandler
{
	public string Name { get; }
	public Uri CallbackUri { get; }
	public Task<WebAuthenticationResult> WaitForCallbackAsync();
}
