using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamJsonRpc;
using Uno.Extensions;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

/// <summary>
/// The server end for the "ide-channel" communication.
/// </summary>
internal class IdeChannelServer : IIdeChannel, IDisposable
{
	private readonly ILogger _logger;
	private readonly IDisposable? _configSubscription;

	private Task<bool> _initializeTask;
	private NamedPipeServerStream? _pipeServer;
	private JsonRpc? _rpcServer;
	private Proxy? _proxy;
	private readonly Timer _keepAliveTimer;

	public IdeChannelServer(ILogger<IdeChannelServer> logger, IOptionsMonitor<IdeChannelServerOptions> config)
	{
		_logger = logger;

		_keepAliveTimer = new Timer(_ =>
		{
			if (_pipeServer?.IsConnected ?? false)
			{
				SendKeepAlive();
			}
			else
			{
				_keepAliveTimer!.Change(Timeout.Infinite, Timeout.Infinite);
			}
		});

		_initializeTask = Task.Run(() => InitializeServer(config.CurrentValue.ChannelId));
		_configSubscription = config.OnChange(opts => _initializeTask = InitializeServer(opts.ChannelId));
	}

	#region IIdeChannel

	/// <inheritdoc />
	public event EventHandler<IdeMessage>? MessageFromIde;

	/// <inheritdoc />
	async Task IIdeChannel.SendToIdeAsync(IdeMessage message, CancellationToken ct)
	{
		await WaitForReady(ct);

		if (_proxy is null)
		{
			this.Log().LogInformation(
				"Received a message {MessageType} to send to the IDE, but there is no connection available for that.",
				message.Scope);
		}
		else
		{
			_proxy.SendToIde(message);
			ScheduleKeepAlive();
		}

		await Task.Yield();
	}

	#endregion

	/// <inheritdoc />
	public async ValueTask<bool> WaitForReady(CancellationToken ct = default)
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
		=> await _initializeTask;
#pragma warning restore VSTHRD003

	/// <summary>
	/// Initialize as dev-server (cf. IdeChannelClient for init as IDE)
	/// </summary>
	private async Task<bool> InitializeServer(Guid channelId)
	{
		try
		{
			// First we remove the proxy to prevent messages being sent while we are re-initializing
			_proxy = null;

			// Dispose any existing server
			_rpcServer?.Dispose();
			if (_pipeServer is { } server)
			{
				server.Disconnect();
				await server.DisposeAsync();
			}
		}
		catch (Exception error)
		{
			_logger.LogWarning(error, "An error occurred while disposing the existing IDE channel server. Continuing initialization.");
		}

		try
		{
			if (channelId == Guid.Empty)
			{
				return false;
			}

			_pipeServer = new NamedPipeServerStream(
				pipeName: channelId.ToString(),
				direction: PipeDirection.InOut,
				maxNumberOfServerInstances: 1,
				transmissionMode: PipeTransmissionMode.Byte,
				options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);

			await _pipeServer.WaitForConnectionAsync();

			_proxy = new(this);
			_rpcServer = JsonRpc.Attach(_pipeServer, _proxy);

			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("IDE channel successfully initialized.");
			}

			ScheduleKeepAlive();

			SendKeepAlive(); // Send a keep-alive message immediately after connection

			return true;
		}
		catch (Exception error)
		{
			if (_logger.IsEnabled(LogLevel.Error))
			{
				_logger.LogError(error, "Failed to init the IDE channel.");
			}

			return false;
		}
	}

	private const int KeepAliveDelay = 10000; // 10 seconds in milliseconds

	private void ScheduleKeepAlive() => _keepAliveTimer.Change(KeepAliveDelay, KeepAliveDelay);

	private void SendKeepAlive() => _proxy?.SendToIde(new KeepAliveIdeMessage("dev-server"));

	/// <inheritdoc />
	public void Dispose()
	{
		_keepAliveTimer.Dispose();
		_configSubscription?.Dispose();
		_rpcServer?.Dispose();
		_pipeServer?.Dispose();
	}

	private class Proxy(IdeChannelServer Owner) : IIdeChannelServer
	{
		/// <inheritdoc />
		public event EventHandler<IdeMessageEnvelope>? MessageFromDevServer;

		/// <inheritdoc />
		public Task SendToDevServerAsync(IdeMessageEnvelope envelope, CancellationToken ct)
		{
			Owner.MessageFromIde?.Invoke(Owner, IdeMessageSerializer.Deserialize(envelope));
			return Task.CompletedTask;
		}

		internal void SendToIde(IdeMessage message)
			=> MessageFromDevServer?.Invoke(this, IdeMessageSerializer.Serialize(message));
	}
}
