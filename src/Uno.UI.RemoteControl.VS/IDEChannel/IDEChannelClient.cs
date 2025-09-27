using System;
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

	public long MessagesReceivedCount { get; private set; }

	public IdeChannelClient(Guid pipeGuid, ILogger logger)
	{
		_logger = logger;
		_pipeGuid = pipeGuid;
	}

	public void ConnectToHost()
	{
		_IDEChannelCancellation?.Cancel();
		_IDEChannelCancellation = new CancellationTokenSource();

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

				await _pipeServer.ConnectAsync(_IDEChannelCancellation.Token);

				_devServer = JsonRpc.Attach<IIdeChannelServer>(_pipeServer);
				_devServer.MessageFromDevServer += ProcessDevServerMessage;

				_ = Task.Run(StartKeepAliveAsync);
			}
			catch (Exception e)
			{
				_logger.Error($"Error creating IDE channel: {e}");
			}
		}, _IDEChannelCancellation.Token);
	}

	public async Task SendToDevServerAsync(IdeMessage message, CancellationToken ct)
	{
		if (_devServer is not null)
		{
			ct = ct.CanBeCanceled && ct != _IDEChannelCancellation!.Token
				? CancellationTokenSource.CreateLinkedTokenSource(ct, _IDEChannelCancellation!.Token).Token
				: _IDEChannelCancellation!.Token;
			await _devServer.SendToDevServerAsync(IdeMessageSerializer.Serialize(message), ct);
		}
	}

	private async Task StartKeepAliveAsync()
	{
		while (_IDEChannelCancellation is { IsCancellationRequested: false })
		{
			await _devServer!.SendToDevServerAsync(IdeMessageSerializer.Serialize(new KeepAliveIdeMessage("IDE")), default);

			await Task.Delay(5000, _IDEChannelCancellation.Token);
		}
	}

	private void ProcessDevServerMessage(object sender, IdeMessageEnvelope devServerMessageEnvelope)
	{
		try
		{
			MessagesReceivedCount++;

			var devServerMessage = IdeMessageSerializer.Deserialize(devServerMessageEnvelope);

			_logger.Verbose($"IDE: IDEChannel message received {devServerMessage}");

			var process = Task.CompletedTask;
			switch (devServerMessage)
			{
				case KeepAliveIdeMessage:
					_logger.Verbose($"Keep alive from Dev Server");
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
		_pipeServer?.Dispose();
	}
}
