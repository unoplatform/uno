using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreamJsonRpc;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

/// <summary>
/// The server end for the "ide-channel" communication.
/// Uses an atomic <see cref="ChannelSession"/> swap (Interlocked.Exchange) to
/// guarantee lock-free, race-free rebinding across concurrent HTTP requests,
/// IOptionsMonitor callbacks, and inter-process named-pipe coordination.
/// </summary>
internal class IdeChannelServer : IIdeChannel, IIdeChannelManager, IDisposable
{
	private readonly ILogger _logger;
	private readonly IDisposable? _configSubscription;
	private readonly Timer _keepAliveTimer;

	/// <summary>
	/// The single source of truth for the active IDE channel.
	/// All reads go through <see cref="Volatile.Read{T}(ref T)"/>;
	/// all replacements go through <see cref="Interlocked.Exchange{T}(ref T, T)"/>.
	/// </summary>
	private ChannelSession? _session;

	internal bool IsConnected => Volatile.Read(ref _session)?.IsConnected ?? false;

	string? IIdeChannelManager.ChannelId => Volatile.Read(ref _session)?.ChannelId;

	bool IIdeChannelManager.IsConnected => IsConnected;

	public IdeChannelServer(ILogger<IdeChannelServer> logger, IOptionsMonitor<IdeChannelServerOptions> config)
	{
		_logger = logger;

		_keepAliveTimer = new Timer(_ =>
		{
			var session = Volatile.Read(ref _session);
			if (session is { IsConnected: true, Proxy: { } proxy })
			{
				proxy.SendToIde(new KeepAliveIdeMessage("dev-server"));
			}
			else
			{
				_keepAliveTimer!.Change(Timeout.Infinite, Timeout.Infinite);
			}
		});

		_ = ConfigureChannelAsync(config.CurrentValue.ChannelId);
		_configSubscription = config.OnChange(opts => _ = ConfigureChannelAsync(opts.ChannelId));
	}

	#region IIdeChannel

	/// <inheritdoc />
	public event EventHandler<IdeMessage>? MessageFromIde;

	async Task IIdeChannel.SendToIdeAsync(IdeMessage message, CancellationToken ct)
		=> await ((IIdeChannel)this).TrySendToIdeAsync(message, ct);

	/// <inheritdoc />
	async Task<bool> IIdeChannel.TrySendToIdeAsync(IdeMessage message, CancellationToken ct)
	{
		if (!await WaitForReady(ct))
		{
			_logger.LogInformation(
				"Received a message {MessageType} to send to the IDE, but there is no connection available for that.",
				message.Scope);

			return false;
		}

		var session = Volatile.Read(ref _session);
		if (session?.Proxy is not { } proxy)
		{
			return false;
		}

		proxy.SendToIde(message);
		ScheduleKeepAlive();
		return true;
	}
	#endregion

	/// <inheritdoc />
	public async ValueTask<bool> WaitForReady(CancellationToken ct = default)
	{
		var session = Volatile.Read(ref _session);
		if (session is null)
		{
			return false;
		}

		// WaitAsync honours the caller's CT for timeout/cancellation
		// without linking it into the session's own CTS.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
		return await session.ReadyTask.WaitAsync(ct);
#pragma warning restore VSTHRD003
	}

	Task<bool> IIdeChannelManager.RebindAsync(string? channelId)
		=> ConfigureChannelAsync(channelId);

	/// <summary>
	/// Atomically swaps to a new <see cref="ChannelSession"/> for the given
	/// <paramref name="channelId"/>. The previous session (if any) is disposed
	/// asynchronously after the swap. Returns <see langword="true"/> when a
	/// pipe listener is active for the requested channel.
	/// </summary>
	private async Task<bool> ConfigureChannelAsync(string? channelId)
	{
		// Idempotency: if the current session already serves this channel, no-op.
		var current = Volatile.Read(ref _session);
		if (current is not null
			&& current.ChannelId == channelId
			&& !current.Cts.IsCancellationRequested)
		{
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("IDE channel {ChannelId} is already active, skipping rebind.", channelId);
			}

			return true;
		}

		if (string.IsNullOrWhiteSpace(channelId))
		{
			var old = Interlocked.Exchange(ref _session, null);
			if (old is not null)
			{
				await DisposeSessionAsync(old);
			}

			return false;
		}

		ChannelSession newSession;
		try
		{
			newSession = new ChannelSession(channelId);
		}
		catch (Exception error)
		{
			_logger.LogError(error, "Failed to create IDE channel pipe for {ChannelId}.", channelId);
			return false;
		}

		// Atomic swap — last writer wins.
		var previous = Interlocked.Exchange(ref _session, newSession);

		// Launch the background wait for client connection, capturing the session snapshot.
		_ = WaitForClientConnectionAsync(newSession);

		// Dispose previous session asynchronously (cancels its WaitForConnection).
		if (previous is not null)
		{
			_ = DisposeSessionAsync(previous);
		}

		if (_logger.IsEnabled(LogLevel.Debug))
		{
			_logger.LogDebug("IDE channel configured for {ChannelId}.", channelId);
		}

		return true;
	}

	private async Task WaitForClientConnectionAsync(ChannelSession session)
	{
		try
		{
			await session.PipeServer.WaitForConnectionAsync(session.Cts.Token);

			// Verify this session is still the active one.
			if (!ReferenceEquals(Volatile.Read(ref _session), session))
			{
				_logger.LogDebug("IDE channel session was superseded during connection handshake.");
				session.ReadyTcs.TrySetResult(false);
				return;
			}

			session.Proxy = new Proxy(this);
			session.RpcServer = JsonRpc.Attach(session.PipeServer, session.Proxy);
			session.ReadyTcs.TrySetResult(true);

			if (_logger.IsEnabled(LogLevel.Debug))
			{
				_logger.LogDebug("IDE channel {ChannelId} successfully connected.", session.ChannelId);
			}

			ScheduleKeepAlive();
			session.Proxy.SendToIde(new KeepAliveIdeMessage("dev-server"));
		}
		catch (OperationCanceledException)
		{
			session.ReadyTcs.TrySetResult(false);
		}
		catch (Exception error)
		{
			_logger.LogError(error, "Failed to init the IDE channel {ChannelId}.", session.ChannelId);
			session.ReadyTcs.TrySetResult(false);
		}
	}

	private async Task DisposeSessionAsync(ChannelSession session)
	{
		try
		{
			await session.DisposeAsync();
		}
		catch (Exception error)
		{
			_logger.LogWarning(error, "Error while disposing IDE channel session {ChannelId}.", session.ChannelId);
		}
	}

	private const int KeepAliveDelay = 10000; // 10 seconds

	private void ScheduleKeepAlive() => _keepAliveTimer.Change(KeepAliveDelay, KeepAliveDelay);

	/// <inheritdoc />
	public void Dispose()
	{
#pragma warning disable VSTHRD103 // Dispose is synchronous; cancelling pending waits is a best-effort cleanup.
		_keepAliveTimer.Dispose();
		_configSubscription?.Dispose();
		var session = Interlocked.Exchange(ref _session, null);
		if (session is not null)
		{
			session.Cts.Cancel();
			session.RpcServer?.Dispose();
			try
			{
				if (session.PipeServer.IsConnected)
				{
					session.PipeServer.Disconnect();
				}
			}
			catch (InvalidOperationException)
			{
				// The pipe might not have completed its connection handshake yet.
			}

			session.PipeServer.Dispose();
			session.Cts.Dispose();
		}
#pragma warning restore VSTHRD103
	}

	/// <summary>
	/// Encapsulates all mutable state for a single IDE channel lifetime.
	/// Created atomically (pipe + CTS in constructor); swapped via
	/// <see cref="Interlocked.Exchange{T}(ref T, T)"/>.
	/// </summary>
	private sealed class ChannelSession : IAsyncDisposable
	{
		public string ChannelId { get; }
		public NamedPipeServerStream PipeServer { get; }
		public CancellationTokenSource Cts { get; }
		public TaskCompletionSource<bool> ReadyTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Task<bool> ReadyTask => ReadyTcs.Task;
		public bool IsConnected => PipeServer.IsConnected;

		// Set once after the client connects — only by WaitForClientConnectionAsync.
		public JsonRpc? RpcServer { get; set; }
		public Proxy? Proxy { get; set; }

		public ChannelSession(string channelId)
		{
			ChannelId = channelId;
			PipeServer = new NamedPipeServerStream(
				pipeName: channelId,
				direction: PipeDirection.InOut,
				maxNumberOfServerInstances: 1,
				transmissionMode: PipeTransmissionMode.Byte,
				options: PipeOptions.Asynchronous | PipeOptions.WriteThrough,
				inBufferSize: 8 * 1024 * 1024,
				outBufferSize: 8 * 1024 * 1024);
			Cts = new CancellationTokenSource();
		}

		public async ValueTask DisposeAsync()
		{
			await Cts.CancelAsync();
			ReadyTcs.TrySetResult(false);
			RpcServer?.Dispose();

			try
			{
				if (PipeServer.IsConnected)
				{
					PipeServer.Disconnect();
				}
			}
			catch (InvalidOperationException)
			{
				// The pipe might not have completed its connection handshake yet.
			}

			await PipeServer.DisposeAsync();
			Cts.Dispose();
		}
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
