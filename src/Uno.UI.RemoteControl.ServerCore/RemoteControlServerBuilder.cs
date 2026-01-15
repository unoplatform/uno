using System;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Provides a configurable entry point for building transport-agnostic devserver hosts.
/// </summary>
public sealed class RemoteControlServerBuilder
{
	private readonly IServiceCollection _services;

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteControlServerBuilder"/> class.
	/// </summary>
	/// <param name="services">Service collection provided by the host environment.</param>
	public RemoteControlServerBuilder(IServiceCollection services)
	{
		_services = services ?? throw new ArgumentNullException(nameof(services));
	}

	/// <summary>
	/// Gets the service collection used to configure the devserver host.
	/// </summary>
	public IServiceCollection Services => _services;

	/// <summary>
	/// Builds the devserver host from a service provider controlled by the caller.
	/// </summary>
	/// <param name="serviceProvider">Fully configured provider that will own the server lifetime.</param>
	/// <returns>A <see cref="RemoteControlServerHost"/> instance.</returns>
	public RemoteControlServerHost Build(IServiceProvider serviceProvider)
	{
		if (serviceProvider is null)
		{
			throw new ArgumentNullException(nameof(serviceProvider));
		}

		return new RemoteControlServerHost(serviceProvider);
	}
}
