using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.VS.IdeChannel;

internal class IdeChannelClient
{
	private NamedPipeClientStream? _pipeServer;
	private Guid _pipeGuid;
	private CancellationTokenSource? _IDEChannelCancellation;
	private Task? _connectTask;
	private IIdeChannelServer? _devServer;
	private readonly ILogger _logger;

	public event AsyncEventHandler<IdeMessage>? OnMessageReceived;
	public event EventHandler? Connected;

	public long MessagesReceivedCount { get; private set; }

	public IdeChannelClient(Guid pipeGuid, ILogger logger)
	{
		_logger = logger;
		_pipeGuid = pipeGuid;
	}

	public void ConnectToHost()
	{
		_IDEChannelCancellation?.Cancel();
		var cts = new CancellationTokenSource();
		_IDEChannelCancellation = cts;

		var ct = cts.Token;

		_connectTask = Task.Run(async () =>
		{
			try
			{
				_pipeServer = new NamedPipeClientStream(
						serverName: ".",
						pipeName: _pipeGuid.ToString(),
						direction: PipeDirection.InOut,
						options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);

				_logger.Debug($"Creating IDE Channel to Dev Server ({_pipeGuid})");

				await _pipeServer.ConnectAsync(ct);

				_devServer = JsonRpc.Attach<IIdeChannelServer>(_pipeServer);
				_devServer.MessageFromDevServer += ProcessDevServerMessage;

				ScheduleKeepAlive(ct);
			}
			catch (Exception e)
			{
				_logger.Error($"Error creating IDE channel: {e}");
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
		Connected?.Invoke(this, EventArgs.Empty);

		// Replace recursive re-scheduling with a single periodic timer
		var oldTimer = Interlocked.Exchange(ref _keepAliveTimer, null);
		oldTimer?.Dispose();

		var timer = new Timer(state =>
		{
			if (ct is { IsCancellationRequested: false } && _devServer is not null)
			{
				_ = _devServer.SendToDevServerAsync(IdeMessageSerializer.Serialize(new KeepAliveIdeMessage("IDE")), ct);
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

	private void ProcessDevServerMessage(object sender, IdeMessageEnvelope devServerMessageEnvelope)
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
		_pipeServer?.Dispose();
	}
}
