using Microsoft.Extensions.DependencyInjection;

namespace Yllibed.Handlers.Uno.Extensions;
public static class OAuthCallbackExtensions
{
	/// <summary>
	/// Adds the <see cref="OAuthCallbackHandler"/> to the service collection and registers it as an <see cref="IHttpHandler"/>.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the handler will be added.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	public static IServiceCollection AddOAuthCallbackHandler(this IServiceCollection services)
	{
		services.AddSingleton(sp => new OAuthCallbackHandler(sp.GetRequiredService<IOptions<AuthCallbackHandlerOptions>>()));
		services.AddSingleton<IAuthCallbackHandler>(sp => sp.GetRequiredService<OAuthCallbackHandler>()); // Register as IAuthCallbackHandler so eventual consumers can get it as expected Interface,
																										  // which provides the callback awaiting functionality at registration in WebAuthenticationBrokerProvider
		return services;
	}
	/// <summary>
	/// Registers an <see cref="OAuthCallbackHandler"/> Keyed Singleton and its associated dependencies in the service collection with the specified name as Options key and service key.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection"/> to which the handler and dependencies will be added.</param>
	/// <param name="name">The name used to retrieve the <see cref="AuthCallbackHandlerOptions"/> configuration.<br/>
	/// Defaults to <see cref="AuthCallbackHandlerOptions.DefaultName"/>.</param>
	/// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
	public static IServiceCollection AddOAuthCallbackHandler(this IServiceCollection services, string name = AuthCallbackHandlerOptions.DefaultName)
	{
		services.AddSingleton(sp => new OAuthCallbackHandler(sp.GetRequiredService<IOptionsSnapshot<AuthCallbackHandlerOptions>>().Get(name)));
		services.AddKeyedSingleton<IAuthCallbackHandler>(name, (sp, _) => sp.GetRequiredService<OAuthCallbackHandler>()); // Register as IAuthCallbackHandler so eventual consumers can get it as expected Interface,
																														  // which provides the callback awaiting functionality at registration in WebAuthenticationBrokerProvider
		return services;
	}

	public static IServiceCollection AddOAuthCallbackHandler<TService>(this IServiceCollection services, Action<AuthCallbackHandlerOptions> configureOptions)
		where TService : class, IAuthCallbackHandler
	{
		services.Configure(configureOptions);
		return services.AddOAuthCallbackHandler();
	}
	public static IServiceCollection AddOAuthCallbackHandlerAndRegister<TService>(this IServiceCollection services, string name = AuthCallbackHandlerOptions.DefaultName, Action<AuthCallbackHandlerOptions>? configureOptions = null)
		where TService : class, IAuthCallbackHandler
	{
		if (configureOptions != null)
		{
			services.Configure(configureOptions);
		}
		services.AddOAuthCallbackHandler(name);
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
