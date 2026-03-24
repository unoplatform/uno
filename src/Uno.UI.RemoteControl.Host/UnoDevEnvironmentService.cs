using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host;

public class UnoDevEnvironmentService(IIdeChannel ideChannel, AddInsStatus addIns) : BackgroundService
{
	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var udeiMessage = BuildStatusMessage();

		// Try to send immediately (works when IDE channel was configured at startup).
		if (await ideChannel.TrySendToIdeAsync(udeiMessage, stoppingToken))
		{
			return;
		}

		// The IDE channel is not ready yet (e.g. Host was started by MCP without
		// --ideChannel and the channel will be created later via rebind).
		// Wait for the first incoming IDE message (typically a KeepAlive) as a
		// signal that the channel is now connected, then publish the status.
		var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		void OnMessage(object? sender, IdeMessage msg) => connected.TrySetResult();

		ideChannel.MessageFromIde += OnMessage;
		try
		{
			await connected.Task.WaitAsync(stoppingToken);
			await ideChannel.SendToIdeAsync(udeiMessage, stoppingToken);
		}
		finally
		{
			ideChannel.MessageFromIde -= OnMessage;
		}
	}

	private DevelopmentEnvironmentStatusIdeMessage BuildStatusMessage()
	{
		if (addIns.Discovery is null or { Error.Length: > 0 } or { AddIns: null }) // Count: 0 is considered as a valid result!
		{
			return DevelopmentEnvironmentStatusIdeMessage.DevServer.FailedToDiscoverAddIns(addIns.Discovery?.Error);
		}

		if (addIns is { Assemblies: null } or { Discovery.AddIns.Count: > 0, Assemblies.Count: 0 }
			|| addIns.Assemblies.Any(result => result.Error is not null))
		{
			return DevelopmentEnvironmentStatusIdeMessage.DevServer.FailedToLoadAddIns(addIns.Discovery?.Error);
		}

		return DevelopmentEnvironmentStatusIdeMessage.DevServer.Ready;
	}
}
