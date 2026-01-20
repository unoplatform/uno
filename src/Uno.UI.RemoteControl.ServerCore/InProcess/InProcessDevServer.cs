#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.Messaging;
using Uno.UI.RemoteControl.Server;
using Uno.UI.RemoteControl.Server.AppLaunch;
using Uno.UI.RemoteControl.ServerCore;
using Uno.UI.RemoteControl.ServerCore.Configuration;
using Uno.UI.RemoteControl.Services;

namespace DevServerCore;

/// <summary>
/// Public helper that wires up the Remote Control devserver core with sane defaults and exposes
/// an in-process connection API returning <see cref="IFrameTransport"/> instances for runtimes.
/// </summary>
public sealed class InProcessDevServer : IAsyncDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly RemoteControlServerHost _serverHost;
	private readonly Dictionary<ConnectionLease, InProcessRemoteControlServerHost> _connections = new();
	private readonly object _gate = new();
	private readonly RemoteControlConnectionDescriptor _defaultDescriptor;
	private bool _disposed;

	private InProcessDevServer(
		IServiceProvider serviceProvider,
		RemoteControlServerHost serverHost,
		IIdeChannel ideChannel,
		RemoteControlConnectionDescriptor defaultDescriptor)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_serverHost = serverHost ?? throw new ArgumentNullException(nameof(serverHost));
		IdeChannel = ideChannel ?? throw new ArgumentNullException(nameof(ideChannel));
		_defaultDescriptor = defaultDescriptor;
	}

	/// <summary>
	/// Root service provider used by the devserver.
	/// </summary>
	public IServiceProvider Services => _serviceProvider;

	/// <summary>
	/// IDE channel instance used by the devserver. Hosts can use it to emit IDE-originated messages when necessary.
	/// </summary>
	public IIdeChannel IdeChannel { get; }

	/// <summary>
	/// Creates a new devserver helper using the provided configuration callback.
	/// </summary>
	/// <param name="configure">Optional callback used to customize dependency injection registrations.</param>
	/// <param name="ct">Cancellation token observed while initializing the devserver.</param>
	public static InProcessDevServer Create(Action<DevServerCoreHostOptions>? configure = null)
	{
		var options = new DevServerCoreHostOptions();
		configure?.Invoke(options);

		var services = options.Services ?? new ServiceCollection();
		services.AddLogging();

		services.AddSingleton<IRemoteControlConfiguration>(options.RemoteControlConfiguration
			?? new DictionaryRemoteControlConfiguration(options.ConfigurationValues));

		services.AddSingleton<IIdeChannel>(sp =>
			options.IdeChannelFactory?.Invoke(sp)
			?? options.IdeChannel
			?? new LoopbackIdeChannel());

		services.AddSingleton<IApplicationLaunchMonitor>(sp =>
			options.ApplicationLaunchMonitorFactory?.Invoke(sp)
			?? options.ApplicationLaunchMonitor
			?? CreateDefaultLaunchMonitor(sp));

		services.AddScoped<IRemoteControlProcessorFactory>(sp =>
			options.ProcessorFactoryFactory?.Invoke(sp)
			?? CreateDefaultProcessorFactory(sp));

		services.AddScoped<RemoteControlServer>();
		services.AddScoped<IRemoteControlServer>(static sp => sp.GetRequiredService<RemoteControlServer>());
		services.AddScoped<IRemoteControlServerConnection>(static sp => sp.GetRequiredService<RemoteControlServer>());

		options.ConfigureServices?.Invoke(services);

		var provider = options.ServiceProviderFactory?.Invoke(services)
			?? services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = options.ValidateScopes });

		var builder = new RemoteControlServerBuilder(services);
		var host = builder.BuildShared(provider);

		var activeIdeChannel = provider.GetRequiredService<IIdeChannel>();
		var defaultDescriptor = options.DefaultConnectionDescriptor ?? RemoteControlConnectionDescriptor.InProcess;
		return new InProcessDevServer(provider, host, activeIdeChannel, defaultDescriptor);
	}

	/// <summary>
	/// Creates a new in-process connection and returns the client-side transport for the runtime/application.
	/// </summary>
	/// <param name="descriptor">Optional descriptor used for telemetry/log metadata.</param>
	/// <param name="ct">Cancellation token that stops the server loop if triggered before completion.</param>
	public IFrameTransport ConnectApplication(RemoteControlConnectionDescriptor? descriptor = null, CancellationToken ct = default)
	{
		ThrowIfDisposed();

		var connectionHost = InProcessRemoteControlServerHost.Create(_serverHost);
		var lease = new ConnectionLease(this, connectionHost.ClientTransport);

		lock (_gate)
		{
			_connections.Add(lease, connectionHost);
		}

		var metadata = descriptor ?? _defaultDescriptor;
		_ = connectionHost.StartAsync(metadata, ct);

		return lease;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		KeyValuePair<ConnectionLease, InProcessRemoteControlServerHost>[] snapshot;
		lock (_gate)
		{
			snapshot = _connections.ToArray();
			_connections.Clear();
		}

		foreach (var (lease, host) in snapshot)
		{
			lease.TryDisposeTransport();
			await host.DisposeAsync().ConfigureAwait(false);
		}

		await _serverHost.DisposeAsync().ConfigureAwait(false);

		switch (_serviceProvider)
		{
			case IAsyncDisposable asyncDisposable:
				await asyncDisposable.DisposeAsync().ConfigureAwait(false);
				break;
			case IDisposable disposable:
				disposable.Dispose();
				break;
		}
	}

	private ValueTask HandleLeaseDisposedAsync(ConnectionLease lease)
	{
		InProcessRemoteControlServerHost? host = null;

		lock (_gate)
		{
			if (_connections.TryGetValue(lease, out host))
			{
				_connections.Remove(lease);
			}
		}

		return host?.DisposeAsync() ?? ValueTask.CompletedTask;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(InProcessDevServer));
		}
	}

	private static IApplicationLaunchMonitor CreateDefaultLaunchMonitor(IServiceProvider services)
	{
		var monitorType = Type.GetType(
			"Uno.UI.RemoteControl.Server.AppLaunch.ApplicationLaunchMonitor, Uno.UI.RemoteControl.Server",
			throwOnError: false);
		if (monitorType is null)
		{
			return new NullApplicationLaunchMonitor();
		}

		return (IApplicationLaunchMonitor)ActivatorUtilities.CreateInstance(services, monitorType);
	}

	private static IRemoteControlProcessorFactory CreateDefaultProcessorFactory(IServiceProvider services)
	{
		var factoryType = Type.GetType(
			"Uno.UI.RemoteControl.Server.Processors.DefaultRemoteControlProcessorFactory, Uno.UI.RemoteControl.Server",
			throwOnError: false);
		if (factoryType is null)
		{
			throw new InvalidOperationException(
				"DefaultRemoteControlProcessorFactory not found. Reference Uno.UI.RemoteControl.Server or provide ProcessorFactoryFactory in DevServerCoreHostOptions.");
		}

		return (IRemoteControlProcessorFactory)ActivatorUtilities.CreateInstance(services, factoryType);
	}

	private sealed class ConnectionLease : IFrameTransport
	{
		private readonly InProcessDevServer _owner;
		private readonly IFrameTransport _transport;
		private int _disposed;

		public ConnectionLease(InProcessDevServer owner, IFrameTransport transport)
		{
			_owner = owner;
			_transport = transport;
		}

		public bool IsConnected => _transport.IsConnected;

		public Task<Frame?> ReceiveAsync(CancellationToken ct)
			=> _transport.ReceiveAsync(ct);

		public Task SendAsync(Frame frame, CancellationToken ct)
			=> _transport.SendAsync(frame, ct);

		public Task CloseAsync()
			=> _transport.CloseAsync();

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
			{
				return;
			}

			_transport.Dispose();
			_ = _owner.HandleLeaseDisposedAsync(this).AsTask();
		}

		internal void TryDisposeTransport()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
			{
				return;
			}

			_transport.Dispose();
		}
	}
}
