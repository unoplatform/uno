using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
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

	private async Task StartKeepaliveAsync()
	{
		while (_IDEChannelCancellation is { IsCancellationRequested: false })
		{
			_roslynServer?.SendToDevServerAsync(new KeepAliveIdeMessage());

			await Task.Delay(5000);
		}
	}

	private void ProcessDevServerMessage(object sender, IdeMessage devServerMessage)
	{
		_logger.Verbose($"IDE: IDEChannel message received {devServerMessage}");

		if (devServerMessage is ForceHotReloadIdeMessage)
		{
			_logger.Debug($"Hot reload requested");
		}
		else if (devServerMessage is KeepAliveIdeMessage)
		{
			_logger.Verbose($"Keep alive from Dev Server");
		}
		else
		{
			_logger.Verbose($"Unknown message type {devServerMessage?.GetType()} from DevServer");
		}
	}

	internal void Dispose()
	{
		_IDEChannelCancellation?.Cancel();
		_pipeServer?.Dispose();
	}
}
