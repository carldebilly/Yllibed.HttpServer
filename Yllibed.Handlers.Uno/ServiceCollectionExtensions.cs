using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Yllibed.Handlers.Uno;
public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddYllibedAuthCallbackHandler(this IServiceCollection services)
	{
		services.AddSingleton<OAuthCallbackHandler>();
		return services;
	}
	public static IServiceCollection AddYllibedAuthCallbackHandler<TService>(this IServiceCollection services, Action<AuthCallbackHandlerOptions> configureOptions)
		where TService : class, IAuthCallbackHandler
	{
		services.Configure(configureOptions);
		services.AddSingleton<IAuthCallbackHandler, TService>();
		return services;
	}
}
