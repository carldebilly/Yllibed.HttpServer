using Microsoft.Extensions.DependencyInjection;

namespace Yllibed.Handlers.Uno;
public static class OAuthCallbackExtensions
{
	/// <summary>
	/// Adds the <see cref="OAuthCallbackHandler"/> to the service collection and registers it as an <see cref="IHttpHandler"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the handler will be added.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	public static IServiceCollection AddOAuthCallbackHandler(this IServiceCollection services)
	{
		services.AddSingleton<OAuthCallbackHandler>(sp => new OAuthCallbackHandler(sp.GetRequiredService<IOptions<AuthCallbackHandlerOptions>>()));
		services.AddSingleton<IAuthCallbackHandler>(sp => sp.GetRequiredService<OAuthCallbackHandler>()); // Register as IAuthCallbackHandler so eventual consumers can get it as expected Interface,
																										  // which provides the callback awaiting functionality at registration in WebAuthenticationBrokerProvider
		return services;
	}
	public static IServiceCollection AddOAuthCallbackHandler<TService>(this IServiceCollection services, Action<AuthCallbackHandlerOptions> configureOptions)
		where TService : class, IAuthCallbackHandler
	{
		services.Configure(configureOptions);
		return services.AddOAuthCallbackHandler();
	}
	public static IServiceCollection AddOAuthCallbackHandlerAndRegister<TService>(this IServiceCollection services, Action<AuthCallbackHandlerOptions>? configureOptions = null)
		where TService : class, IAuthCallbackHandler
	{
		if (configureOptions != null)
		{
			services.Configure(configureOptions);
		}
		services.AddOAuthCallbackHandler();
		// Register a singleton that wires the handler into the server on construction
		services.AddSingleton<OAuthCallbackHandlerRegistration>();
		return services;
	}
	private sealed class OAuthCallbackHandlerRegistration : IDisposable
	{
		private readonly IDisposable _registration;
		public OAuthCallbackHandlerRegistration(Server server, OAuthCallbackHandler handler) =>
			// Place first by registering now; Server keeps order of registration
			_registration = server.RegisterHandler(handler);
		public void Dispose() => _registration.Dispose();
	}
}
