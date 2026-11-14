using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using ILogger = Uno.UI.RemoteControl.VS.Helpers.ILogger;

namespace Uno.UI.RemoteControl.VS.IdeChannel;

internal class IdeChannelClient
{
	private NamedPipeClientStream? _pipeServer;
	private Guid _pipeGuid;
	private CancellationTokenSource? _IDEChannelCancellation;
	private Task? _connectTask;
	private IIdeChannelServer? _devServer;
	private readonly ILogger _logger;
	private readonly TimeSpan _connectTimeout;

	// Safety net for a host that stays alive but never creates the pipe: a host that exits (or is replaced or stopped) ends the wait earlier, EntryPoint disposes the client.
	private static readonly TimeSpan PipeWaitTimeout = TimeSpan.FromMinutes(10);
	private const int ConnectAttemptTimeoutMs = 2000;
	private const int ProbeInitialDelayMs = 100;
	private const int ProbeMaxDelayMs = 500;
	private const int ErrorFileNotFound = 2;

	[DllImport("kernel32.dll", EntryPoint = "WaitNamedPipeW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool WaitNamedPipe(string name, int timeout);

	public event AsyncEventHandler<IdeMessage>? OnMessageReceived;
	public event EventHandler? Connected;

	public long MessagesReceivedCount { get; private set; }

	internal Task? ConnectTask => _connectTask;

	public IdeChannelClient(Guid pipeGuid, ILogger logger, TimeSpan? connectTimeout = null)
	{
		_logger = logger;
		_pipeGuid = pipeGuid;
		_connectTimeout = connectTimeout ?? PipeWaitTimeout;
	}

	/// <summary>
	/// Tells whether the pipe may already have been created by the server, without ever opening it
	/// (unlike File.Exists, see dotnet/runtime#69604): the host serves a single pipe instance and
	/// listens only once, so a probe that connects would leave the real client stuck on ERROR_PIPE_BUSY.
	/// </summary>
	internal static bool PipeMayExist(string pipeName)
	{
		try
		{
			// Returns immediately with ERROR_FILE_NOT_FOUND while the pipe does not exist.
			return WaitNamedPipe(@"\\.\pipe\" + pipeName, 1) || Marshal.GetLastWin32Error() != ErrorFileNotFound;
		}
		catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
		{
			return true;
		}
	}

	// Synchronous on purpose: Stream.DisposeAsync does not exist on .NET Framework (the Visual Studio extension), and the VSTHRD103 analyzer asks for it where it does exist (the net11 test project).
	private static void DisposeSynchronously(IDisposable? disposable) => disposable?.Dispose();

	public void ConnectToHost()
	{
		_IDEChannelCancellation?.Cancel();
		var cts = new CancellationTokenSource();
		_IDEChannelCancellation = cts;

		var ct = cts.Token;

		_connectTask = Task.Run(async () =>
		{
			NamedPipeClientStream? pipe = null;
			try
			{
				_logger.Debug($"Creating IDE Channel to Dev Server ({_pipeGuid})");

				// On .NET Framework ConnectAsync busy-spins a pool thread (WaitNamedPipe returns at once + SpinOnce)
				// while the pipe does not exist, so we wait with Task.Delay and only connect once the pipe exists.
				var pipeName = _pipeGuid.ToString();
				var elapsed = Stopwatch.StartNew();
				var delay = ProbeInitialDelayMs;
				while (true)
				{
					ct.ThrowIfCancellationRequested();

					if (PipeMayExist(pipeName))
					{
						pipe = new NamedPipeClientStream(
							serverName: ".",
							pipeName: pipeName,
							direction: PipeDirection.InOut,
							options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);

						try
						{
							await pipe.ConnectAsync(ConnectAttemptTimeoutMs, ct);
							break;
						}
						catch (TimeoutException)
						{
							DisposeSynchronously(pipe);
							pipe = null;
						}
					}

					if (elapsed.Elapsed >= _connectTimeout)
					{
						_logger.Warn($"IDE Channel pipe ({_pipeGuid}) could not be connected within {(int)_connectTimeout.TotalSeconds}s (the pipe was not created or is not accepting connections), giving up (the dev-server host probably failed to start)");
						return;
					}

					await Task.Delay(delay, ct);
					delay = Math.Min(delay * 2, ProbeMaxDelayMs);
				}

				// Publish the pipe, then check for cancellation: either Dispose sees the pipe and closes it, or we see the cancellation and the finally block closes it.
				Interlocked.Exchange(ref _pipeServer, pipe);
				if (ct.IsCancellationRequested)
				{
					return;
				}

				var connected = pipe;
				pipe = null; // ownership moved to _pipeServer

				_devServer = JsonRpc.Attach<IIdeChannelServer>(connected);
				_devServer.MessageFromDevServer += ProcessDevServerMessage;

				// Signal connection established once
				Connected?.Invoke(this, EventArgs.Empty);
				ScheduleKeepAlive(ct);
			}
			catch (Exception e) when (ct.IsCancellationRequested)
			{
				// Disposed (replaced, host exited or stopped) while connecting, whatever it made fail: nothing to report
				_logger.Debug($"IDE Channel connection ({_pipeGuid}) cancelled ({e.GetType().Name})");
			}
			catch (Exception e)
			{
				_logger.Error($"Error creating IDE channel: {e}");
			}
			finally
			{
				DisposeSynchronously(pipe);
			}
		}, ct);
	}

	public async Task SendToDevServerAsync(IdeMessage message, CancellationToken ct)
	{
		if (_devServer is not null)
		{
			ct = ct.CanBeCanceled && ct != _IDEChannelCancellation!.Token
				? CancellationTokenSource.CreateLinkedTokenSource(ct, _IDEChannelCancellation!.Token).Token
				: _IDEChannelCancellation!.Token;
			await _devServer.SendToDevServerAsync(IdeMessageSerializer.Serialize(message), ct);
			ScheduleKeepAlive(_IDEChannelCancellation!.Token);
		}
	}

	private const int KeepAliveDelay = 10000; // 10 seconds in milliseconds
	private Timer? _keepAliveTimer;

	private void ScheduleKeepAlive(CancellationToken ct)
	{
		// Replace recursive re-scheduling with a single periodic timer
		var oldTimer = Interlocked.Exchange(ref _keepAliveTimer, null);
		oldTimer?.Dispose();

		var timer = new Timer(state =>
		{
			if (ct is { IsCancellationRequested: false } && _devServer is not null)
			{
				try
				{
					_ = _devServer
						.SendToDevServerAsync(IdeMessageSerializer.Serialize(new KeepAliveIdeMessage("IDE")), ct)
						.ContinueWith(t =>
						{
							if (t.IsFaulted && t.Exception is { } ex)
							{
								_logger.Verbose($"Keep-alive send failed: {ex.Flatten().Message}");
							}
						}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
				}
				catch (Exception ex)
				{
					_logger.Verbose($"Keep-alive send exception: {ex.Message}");
				}
			}
			else
			{
				// Stop the timer when disconnected or canceled
				Interlocked.Exchange(ref _keepAliveTimer, null)?.Dispose();
			}
		}, null, KeepAliveDelay, KeepAliveDelay);

		if (Interlocked.CompareExchange(ref _keepAliveTimer, timer, null) is not null)
		{
			timer.Dispose();
		}
	}

	private void ProcessDevServerMessage(object? sender, IdeMessageEnvelope devServerMessageEnvelope)
	{
		try
		{
			MessagesReceivedCount++;

			var devServerMessage = IdeMessageSerializer.Deserialize(devServerMessageEnvelope);

			var process = Task.CompletedTask;
			switch (devServerMessage)
			{
				case KeepAliveIdeMessage ka:
					_logger.Verbose($"Keep alive from {ka.Source}");
					break;
				case IdeMessage message:
					_logger.Verbose($"Dev Server Message {message.GetType()} requested");
					process = OnMessageReceived.InvokeAsync(this, message);
					break;
				default:
					_logger.Verbose($"Unknown message type {devServerMessage?.GetType()} from DevServer");
					break;
			}

			_ = process.ContinueWith(
				t => _logger.Error($"Failed to process message {devServerMessage}: {t.Exception?.Flatten()}"),
				_IDEChannelCancellation!.Token,
				TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.AttachedToParent,
				TaskScheduler.Default);
		}
		catch (Exception e)
		{
			_logger.Error($"Error processing message from DevServer: {e}");
		}
	}

	internal void Dispose()
	{
		_IDEChannelCancellation?.Cancel();
		_keepAliveTimer?.Dispose();

		if (_devServer is not null)
		{
			_devServer.MessageFromDevServer -= ProcessDevServerMessage;
		}

		_pipeServer?.Dispose();
	}
}
