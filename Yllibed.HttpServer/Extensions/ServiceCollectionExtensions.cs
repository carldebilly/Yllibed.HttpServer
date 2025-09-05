using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Yllibed.HttpServer.Extensions;

/// <summary>
/// Extension methods for registering Yllibed.HttpServer with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds the Yllibed HttpServer to the service collection using IOptions&lt;ServerOptions&gt; configuration.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddYllibedHttpServer(this IServiceCollection services)
	{
		services.AddSingleton<Server>(sp => new Server(sp.GetRequiredService<IOptions<ServerOptions>>()));
		return services;
	}

	/// <summary>
	/// Adds the Yllibed HttpServer to the service collection with configuration.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="configureOptions">Configure the server options.</param>
	/// <returns>The service collection for chaining.</returns>
	public static IServiceCollection AddYllibedHttpServer(this IServiceCollection services, Action<ServerOptions> configureOptions)
	{
		services.Configure(configureOptions);
		services.AddSingleton<Server>(sp => new Server(sp.GetRequiredService<IOptions<ServerOptions>>()));
		return services;
	}
}
