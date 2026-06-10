using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.ServerCore;
using Uno.UI.RemoteControl.ServerCore.Configuration;
using Uno.UI.RemoteControl.Services;

namespace DevServerCore;

/// <summary>
/// Options used to bootstrap <see cref="InProcessDevServer"/>.
/// </summary>
public sealed class DevServerCoreHostOptions
{
	/// <summary>
	/// Optional service collection to reuse. When null, a fresh collection is created.
	/// </summary>
	public IServiceCollection? Services { get; set; }

	/// <summary>
	/// Controls whether the default provider validates scopes.
	/// </summary>
	public bool ValidateScopes { get; set; } = true;

	/// <summary>
	/// Configuration values exposed to <see cref="IRemoteControlConfiguration"/> when no custom implementation is provided.
	/// </summary>
	public IDictionary<string, string?> ConfigurationValues { get; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Optional configuration implementation to expose directly to the devserver.
	/// </summary>
	public IRemoteControlConfiguration? RemoteControlConfiguration { get; set; }

	/// <summary>
	/// Optional IDE channel instance override. When null, a loopback channel is used.
	/// </summary>
	public IIdeChannel? IdeChannel { get; set; }

	/// <summary>
	/// Factory used to create the IDE channel when <see cref="IdeChannel"/> is not provided.
	/// </summary>
	public Func<IServiceProvider, IIdeChannel>? IdeChannelFactory { get; set; }

	/// <summary>
	/// Optional launch monitor override. When null, a default implementation is resolved.
	/// </summary>
	public IApplicationLaunchMonitor? ApplicationLaunchMonitor { get; set; }

	/// <summary>
	/// Factory used to create the launch monitor when <see cref="ApplicationLaunchMonitor"/> is not provided.
	/// </summary>
	public Func<IServiceProvider, IApplicationLaunchMonitor>? ApplicationLaunchMonitorFactory { get; set; }

	/// <summary>
	/// Factory used to create the processor factory when no override is provided.
	/// </summary>
	public Func<IServiceProvider, IRemoteControlProcessorFactory>? ProcessorFactoryFactory { get; set; }

	/// <summary>
	/// Callback invoked after default registrations to customize the service collection.
	/// </summary>
	public Action<IServiceCollection>? ConfigureServices { get; set; }

	/// <summary>
	/// Optional factory used to create the root <see cref="IServiceProvider"/>.
	/// </summary>
	public Func<IServiceCollection, IServiceProvider>? ServiceProviderFactory { get; set; }

	/// <summary>
	/// Default descriptor applied when <see cref="InProcessDevServer.ConnectApplication"/> is invoked without metadata.
	/// </summary>
	public RemoteControlConnectionDescriptor? DefaultConnectionDescriptor { get; set; }
}
