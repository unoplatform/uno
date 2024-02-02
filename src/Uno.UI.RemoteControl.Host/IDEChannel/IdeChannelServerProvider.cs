using System;
using System.IO.Pipes;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Host.IdeChannel;

internal class IdeChannelServerProvider : IIdeChannelServerProvider
{
	private readonly ILogger _logger;
	private readonly IConfiguration _configuration;
	private readonly Task<IdeChannelServer?> _initializeTask;
	private NamedPipeServerStream? _pipeServer;
	private IdeChannelServer? _ideChannelServer;
	private JsonRpc? _rpcServer;

	public IdeChannelServerProvider(ILogger<IdeChannelServerProvider> logger, IConfiguration configuration)
	{
		_logger = logger;
		_configuration = configuration;

		_initializeTask = Task.Run(Initialize);
	}

	private async Task<IdeChannelServer?> Initialize()
	{
		if (!Guid.TryParse(_configuration["ideChannel"], out var ideChannel))
		{
			_logger.LogDebug("No IDE Channel ID specified, skipping");
			return null;
		}

		_pipeServer = new NamedPipeServerStream(
			pipeName: ideChannel.ToString(),
			direction: PipeDirection.InOut,
			maxNumberOfServerInstances: 1,
			transmissionMode: PipeTransmissionMode.Byte,
			options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);

		await _pipeServer.WaitForConnectionAsync();

		if (_logger.IsEnabled(LogLevel.Debug))
		{
			_logger.LogDebug("IDE Connected");
		}

		_ideChannelServer = new IdeChannelServer();
		_ideChannelServer.MessageFromIDE += OnMessageFromIDE;
		_rpcServer = JsonRpc.Attach(_pipeServer, _ideChannelServer);

		_ = StartKeepaliveAsync();

		return _ideChannelServer;
	}

	private async Task StartKeepaliveAsync()
	{
		while (_pipeServer?.IsConnected ?? false)
		{
			_ideChannelServer?.SendToIdeAsync(new KeepAliveIdeMessage());

			await Task.Delay(5000);
		}
	}

	private void OnMessageFromIDE(object? sender, IdeMessage ideMessage)
	{
		if (ideMessage is KeepAliveIdeMessage)
		{
#if DEBUG
			_logger.LogDebug("Keepalive from IDE");
#endif
		}
		else
		{
			_logger.LogDebug($"Unknown message type {ideMessage?.GetType()} from IDE");
		}
	}

	public async Task<IdeChannelServer?> GetIdeChannelServerAsync()
	{
#pragma warning disable IDE0022 // Use expression body for method
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
		return await _initializeTask;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
#pragma warning restore IDE0022 // Use expression body for method
	}
}
