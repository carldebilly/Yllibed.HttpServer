using System.Net;

namespace Yllibed.HttpServer;

/// <summary>
/// Configuration options for <see cref="Server"/>.
/// </summary>
public sealed class ServerOptions
{
	/// <summary>
	/// Port number to listen to. 0 = dynamic.
	/// </summary>
	public ushort Port { get; set; }

	/// <summary>
	/// IPv4 address to bind the listener to. Defaults to <see cref="IPAddress.Any"/>.
	/// </summary>
	public IPAddress BindAddress4 { get; set; }

	/// <summary>
	/// IPv6 address to bind the listener to. Defaults to <see cref="IPAddress.IPv6Any"/>.
	/// </summary>
	public IPAddress BindAddress6 { get; set; }

	/// <summary>
	/// Hostname used to compose the public IPv4 URI. Defaults to loopback "127.0.0.1".
	/// </summary>
	public string Hostname4 { get; set; }

	/// <summary>
	/// Hostname used to compose the public IPv6 URI. Defaults to loopback "::1".
	/// </summary>
	public string Hostname6 { get; set; }

	public ServerOptions()
	{
		Port = 0;
		BindAddress4 = IPAddress.Any;
		BindAddress6 = IPAddress.IPv6Any;
		Hostname4 = "127.0.0.1";
		Hostname6 = "::1";
	}
}
