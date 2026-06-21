using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Provides a lightweight wrapper that feeds an in-process <see cref="FrameTransportPair"/> into the devserver host.
/// </summary>
public sealed class InProcessRemoteControlServerHost : IAsyncDisposable
{
	private readonly RemoteControlServerHost _serverHost;
	private readonly FrameTransportPair _transportPair;
	private Task? _runTask;

	private InProcessRemoteControlServerHost(RemoteControlServerHost serverHost, FrameTransportPair transportPair)
	{
		_serverHost = serverHost ?? throw new ArgumentNullException(nameof(serverHost));
		_transportPair = transportPair ?? throw new ArgumentNullException(nameof(transportPair));
	}

	/// <summary>
	/// Creates a new in-process host wrapper and underlying transport pair.
	/// </summary>
	public static InProcessRemoteControlServerHost Create(RemoteControlServerHost serverHost)
		=> new(serverHost, FrameTransportPair.Create());

	/// <summary>
	/// Gets the transport that should be connected to the client/runtime side.
	/// </summary>
	public IFrameTransport ClientTransport => _transportPair.Peer2;

	/// <summary>
	/// Starts the in-process server using the internal transport pair.
	/// </summary>
	/// <param name="descriptor">Optional descriptor to override the default "in-process/loopback" metadata.</param>
	/// <param name="ct">Cancellation token that stops the server loop.</param>
	public Task StartAsync(RemoteControlConnectionDescriptor? descriptor = null, CancellationToken ct = default)
	{
		if (_runTask is not null)
		{
			throw new InvalidOperationException("The in-process host has already been started.");
		}

		_runTask = _serverHost
			.RunConnectionAsync(
				(_, token) => new ValueTask<IFrameTransport>(_transportPair.Peer1),
				descriptor ?? RemoteControlConnectionDescriptor.InProcess,
				ct)
			.AsTask();

		return _runTask;
	}

	/// <summary>
	/// Gets a task that completes when the underlying server connection finishes.
	/// </summary>
	public Task Completion => _runTask ?? Task.CompletedTask;

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_transportPair.Dispose();

		if (_runTask is not null)
		{
			try
			{
				await _runTask.ConfigureAwait(false);
			}
			catch
			{
				// Ignore errors surfaced after disposal; tests can assert explicitly if needed.
			}
		}
	}
}
