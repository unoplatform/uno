using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.UI.RemoteControl.Host;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Represents a running instance of the transport-agnostic devserver host.
/// The host owns a global root container (shared across connections) plus a connection-dependent scope per transport.
/// </summary>
public sealed class RemoteControlServerHost : IAsyncDisposable
{
	private readonly GlobalServiceProviderLease _globalLease;
	private readonly IServiceProvider _serviceProvider;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<RemoteControlServerHost>? _logger;
	private bool _isDisposed;

	internal RemoteControlServerHost(GlobalServiceProviderLease globalLease)
	{
		_globalLease = globalLease ?? throw new ArgumentNullException(nameof(globalLease));
		_serviceProvider = globalLease.ServiceProvider;
		_scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
		_logger = _serviceProvider.GetService<ILogger<RemoteControlServerHost>>();
	}

	/// <summary>
	/// Starts the devserver instance.
	/// </summary>
	/// <param name="ct">Cancellation token used to observe cancellation.</param>
	public ValueTask StartAsync(CancellationToken ct = default)
	{
		ThrowIfDisposed();
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Stops the devserver instance.
	/// </summary>
	/// <param name="ct">Cancellation token used to observe cancellation.</param>
	public ValueTask StopAsync(CancellationToken ct = default)
	{
		ThrowIfDisposed();
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Handles a single connection by creating a scoped server instance and running it on the provided transport.
	/// </summary>
	/// <param name="transportFactory">Factory creating the transport tied to the scoped service provider.</param>
	/// <param name="descriptor">Optional metadata describing the connection for logging/telemetry purposes.</param>
	/// <param name="ct">Cancellation token for the connection lifetime.</param>
	public async ValueTask RunConnectionAsync(
		Func<IServiceProvider, CancellationToken, ValueTask<IFrameTransport>> transportFactory,
		RemoteControlConnectionDescriptor? descriptor = null,
		CancellationToken ct = default)
	{
		if (transportFactory is null)
		{
			throw new ArgumentNullException(nameof(transportFactory));
		}

		ThrowIfDisposed();

		var descriptorValue = descriptor ?? RemoteControlConnectionDescriptor.Empty; // host may omit metadata when it has nothing meaningful

		await using var connectionScope = ConnectionServiceScope.Create(_scopeFactory, descriptorValue, _logger);
		var scopedServices = connectionScope.Services;

		try
		{
			var transport = await transportFactory(scopedServices, ct).ConfigureAwait(false)
				?? throw new InvalidOperationException("Transport factory returned null.");

			connectionScope.AttachTransport(transport);
			connectionScope.LogConnectionStart();

			var connectionHandler = scopedServices.GetRequiredService<IRemoteControlServerConnection>();
			await connectionHandler.HandleConnectionAsync(transport, ct).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			if (_logger?.IsEnabled(LogLevel.Error) == true)
			{
				_logger.LogError(ex, "Devserver connection failed.");
			}
			throw;
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		await _globalLease.DisposeAsync().ConfigureAwait(false);
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(nameof(RemoteControlServerHost));
		}
	}

	private static async ValueTask CloseTransportAsync(IFrameTransport? transport)
	{
		if (transport is null)
		{
			return;
		}

		try
		{
			await transport.CloseAsync().ConfigureAwait(false);
		}
		catch
		{
			// Ignore shutdown errors.
		}

		transport.Dispose();
	}

	/// <summary>
	/// Represents the per-connection service scope ("connection-dependent" services).
	/// </summary>
	private sealed class ConnectionServiceScope : IAsyncDisposable
	{
		private readonly AsyncServiceScope _scope;
		private readonly RemoteControlConnectionDescriptor _descriptor;
		private readonly ILogger<RemoteControlServerHost>? _logger;
		private IFrameTransport? _transport;

		private ConnectionServiceScope(IServiceScopeFactory scopeFactory, RemoteControlConnectionDescriptor descriptor, ILogger<RemoteControlServerHost>? logger)
		{
			_scope = scopeFactory.CreateAsyncScope();
			_descriptor = descriptor;
			_logger = logger;
		}

		public static ConnectionServiceScope Create(
			IServiceScopeFactory scopeFactory,
			RemoteControlConnectionDescriptor descriptor,
			ILogger<RemoteControlServerHost>? logger)
			=> new(scopeFactory, descriptor, logger);

		public IServiceProvider Services => _scope.ServiceProvider;

		public void AttachTransport(IFrameTransport transport)
		{
			_transport = transport ?? throw new ArgumentNullException(nameof(transport));
		}

		public void LogConnectionStart()
		{
			if (_logger?.IsEnabled(LogLevel.Debug) == true)
			{
				_logger.LogDebug(
					"Starting devserver connection ({Transport}/{RemoteEndpoint})",
					_descriptor.TransportName ?? "unknown",
					_descriptor.RemoteEndpoint ?? "n/a");
			}
		}

		public async ValueTask DisposeAsync()
		{
			await CloseTransportAsync(_transport).ConfigureAwait(false);

			if (_logger?.IsEnabled(LogLevel.Debug) == true)
			{
				_logger.LogDebug(
					"Disposing devserver connection ({Transport}/{RemoteEndpoint})",
					_descriptor.TransportName ?? "unknown",
					_descriptor.RemoteEndpoint ?? "n/a");
			}

			await _scope.DisposeAsync().ConfigureAwait(false);
		}
	}
}
