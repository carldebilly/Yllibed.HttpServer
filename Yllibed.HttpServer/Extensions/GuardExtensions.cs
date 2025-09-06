using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Yllibed.HttpServer.Handlers;

namespace Yllibed.HttpServer.Extensions;

/// <summary>
/// Extensions to configure GuardHandler via Microsoft DI.
/// </summary>
public static class GuardExtensions
{
	/// <summary>
	/// Registers GuardHandler with options support and exposes it as both its concrete type and as IHttpHandler.
	/// </summary>
	public static IServiceCollection AddGuardHandler(this IServiceCollection services)
	{
		services.AddSingleton<GuardHandler>(sp => new GuardHandler(sp.GetRequiredService<IOptions<GuardHandler.Options>>()));
		services.AddSingleton<IHttpHandler>(sp => sp.GetRequiredService<GuardHandler>());
		return services;
	}

	/// <summary>
	/// Registers GuardHandler with configuration delegate.
	/// </summary>
	public static IServiceCollection AddGuardHandler(this IServiceCollection services, Action<GuardHandler.Options> configure)
	{
		services.Configure(configure);
		return services.AddGuardHandler();
	}

	/// <summary>
	/// Registers GuardHandler and automatically registers it into the Server pipeline.
	/// This avoids having to resolve the handler manually just to call Server.RegisterHandler.
	/// </summary>
	public static IServiceCollection AddGuardHandlerAndRegister(this IServiceCollection services, Action<GuardHandler.Options>? configure = null)
	{
		if (configure != null)
		{
			services.Configure(configure);
		}
		services.AddGuardHandler();
		// Register a singleton that wires the handler into the server on construction
		services.AddSingleton<GuardRegistration>();
		return services;
	}

	private sealed class GuardRegistration : IDisposable
	{
		private readonly IDisposable _registration;

		public GuardRegistration(Server server, GuardHandler handler)
		{
			// Place first by registering now; Server keeps order of registration
			_registration = server.RegisterHandler(handler);
		}

		public void Dispose()
		{
			_registration.Dispose();
		}
	}
}
