using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using Uno.UI.RemoteControl.Host.IDEChannel;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Helpers;

namespace Uno.UI.RemoteControl.VS.IdeChannel;

internal class IdeChannelClient
{
	private NamedPipeClientStream? _pipeServer;
	private Guid _pipeGuid;
	private CancellationTokenSource? _IDEChannelCancellation;
	private Task? _connectTask;
	private JsonRpc? _rpc;
	private IIdeChannelServer? _roslynServer;
	private readonly ILogger _logger;

	public event AsyncEventHandler<ForceHotReloadIdeMessage>? ForceHotReloadRequested;

	public IdeChannelClient(Guid pipeGuid, ILogger logger)
	{
		_logger = logger;
		_pipeGuid = pipeGuid;
	}

	public void ConnectToHost()
	{
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

				_rpc = JsonRpc.Attach(_pipeServer);
				_rpc.AllowModificationWhileListening = true;
				_roslynServer = _rpc.Attach<IIdeChannelServer>();
				_rpc.AllowModificationWhileListening = false;

				_roslynServer.MessageFromDevServer += ProcessDevServerMessage;

				_ = Task.Run(StartKeepaliveAsync);
			}
			catch (Exception e)
			{
				_logger.Error($"Error creating IDE channel: {e}");
			}
		}, _IDEChannelCancellation.Token);
	}

	public async Task SendToDevServerAsync(IdeMessage message, CancellationToken ct)
	{
		if (_roslynServer is not null)
		{
			ct = ct.CanBeCanceled && ct != _IDEChannelCancellation!.Token
				? CancellationTokenSource.CreateLinkedTokenSource(ct, _IDEChannelCancellation!.Token).Token
				: _IDEChannelCancellation!.Token;
			await _roslynServer.SendToDevServerAsync(IdeMessageSerializer.Serialize(message), ct);
		}
	}

	private async Task StartKeepaliveAsync()
	{
		while (_IDEChannelCancellation is { IsCancellationRequested: false })
		{
			_roslynServer?.SendToDevServerAsync(IdeMessageSerializer.Serialize(new KeepAliveIdeMessage()), _IDEChannelCancellation.Token);

			await Task.Delay(5000, _IDEChannelCancellation.Token);
		}
	}

	private void ProcessDevServerMessage(object sender, IdeMessageEnvelope devServerMessageEnvelope)
	{
		try
		{
			var devServerMessage = IdeMessageSerializer.Deserialize(devServerMessageEnvelope);

			_logger.Verbose($"IDE: IDEChannel message received {devServerMessage}");

			var process = Task.CompletedTask;
			switch (devServerMessage)
			{
				case ForceHotReloadIdeMessage forceHotReloadMessage when ForceHotReloadRequested is { } hrRequested:
					_logger.Debug("Hot reload requested");
					process = hrRequested.InvokeAsync(this, forceHotReloadMessage);
					break;
				case KeepAliveIdeMessage:
					_logger.Verbose($"Keep alive from Dev Server");
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
