using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Yllibed.HttpServer.Handlers;

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
		services.TryAddSingleton<HandlerRegistrationService>();
		services.TryAddSingleton<Server>(sp =>
		{
			var server = new Server(sp.GetRequiredService<IOptions<ServerOptions>>());
			var registrationService = sp.GetRequiredService<HandlerRegistrationService>();
			registrationService.RegisterHandlers(server, sp);
			return server;
		});
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

		// Avoid calling the parameterless overload to prevent confusion/recursion; register the same singletons directly.
		services.TryAddSingleton<HandlerRegistrationService>();
		services.TryAddSingleton<Server>(sp =>
		{
			var server = new Server(sp.GetRequiredService<IOptions<ServerOptions>>());
			var registrationService = sp.GetRequiredService<HandlerRegistrationService>();
			registrationService.RegisterHandlers(server, sp);
			return server;
		});

		return services;
	}

	/// <summary>
	/// Generic: registers a handler THandler and automatically wires it into the Server pipeline.
	/// The handler is resolved from DI and registered when the ServiceProvider is built; disposal unregisters it.
	/// </summary>
	public static IServiceCollection AddHttpHandlerAndRegister<THandler>(this IServiceCollection services)
		where THandler : class, IHttpHandler
	{
		// Ensure handler is registered
		services.TryAddSingleton<THandler>();

		// Ensure the HandlerRegistrationService exists
		services.TryAddSingleton<HandlerRegistrationService>();

		// Register this handler type to be auto-registered
		services.Configure<HandlerRegistrationOptions>(options =>
		{
			options.HandlerTypes.Add(typeof(THandler));
		});

		return services;
	}

	/// <summary>
	/// Generic with options: configures TOptions and registers THandler that consumes IOptions&lt;TOptions&gt; (if applicable),
	/// and auto-wires it into the Server pipeline. This removes the need to resolve it manually for registration.
	/// </summary>
	public static IServiceCollection AddHttpHandlerAndRegister<THandler, TOptions>(this IServiceCollection services, Action<TOptions> configure)
		where THandler : class, IHttpHandler
		where TOptions : class, new()
	{
		services.Configure(configure);
		return services.AddHttpHandlerAndRegister<THandler>();
	}

	private sealed class HandlerRegistrationOptions
	{
		public List<Type> HandlerTypes { get; } = new();
	}

	private sealed class HandlerRegistrationService
	{
		private readonly IOptions<HandlerRegistrationOptions> _options;
		private readonly List<IDisposable> _registrations = new();

		public HandlerRegistrationService(IOptions<HandlerRegistrationOptions> options)
		{
			_options = options;
		}

		public void RegisterHandlers(Server server, IServiceProvider serviceProvider)
		{
			foreach (var handlerType in _options.Value.HandlerTypes)
			{
				var handler = (IHttpHandler)serviceProvider.GetRequiredService(handlerType);
				var registration = server.RegisterHandler(handler);
				_registrations.Add(registration);
			}
		}
	}
}
