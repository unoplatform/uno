using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
	private readonly IConfiguration _configuration;

	private readonly Task<bool> _initializeTask;
	private NamedPipeServerStream? _pipeServer;
	private JsonRpc? _rpcServer;
	private Proxy? _proxy;

	public IdeChannelServer(ILogger<IdeChannelServer> logger, IConfiguration configuration)
	{
		_logger = logger;
		_configuration = configuration;

		_initializeTask = Task.Run(InitializeServer);
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
			this.Log().Log(LogLevel.Information, "Received an message to send to the IDE, but there is no connection available for that.");
		}
		else
		{
			_proxy.SendToIde(message);
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
	private async Task<bool> InitializeServer()
	{
		if (!Guid.TryParse(_configuration["ideChannel"], out var ideChannel))
		{
			_logger.LogDebug("No IDE Channel ID specified, skipping.");
			return false;
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

		_proxy = new(this);
		_rpcServer = JsonRpc.Attach(_pipeServer, _proxy);

		_ = StartKeepAliveAsync();
		return true;
	}

	private async Task StartKeepAliveAsync()
	{
		// Note: The dev-server is expected to send message regularly ... and AS SOON AS POSSIBLE (the Task.Delay is after the first SendToIde()!).
		while (_pipeServer?.IsConnected ?? false)
		{
			_proxy?.SendToIde(new KeepAliveIdeMessage("dev-server"));

			await Task.Delay(5000);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
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
