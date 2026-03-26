using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Uno.UI.RemoteControl.Host.Extensibility;
using Uno.UI.RemoteControl.Host.IdeChannel;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Host;

internal class UnoDevEnvironmentService(
	IIdeChannel ideChannel,
	IIdeChannelManager ideChannelManager,
	ILogger<UnoDevEnvironmentService> logger,
	AddInsStatus addIns) : BackgroundService
{
	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var udeiMessage = BuildStatusMessage();

		// Publish the current environment state on EVERY client connection.
		// This covers: fresh startup, rebind, reconnect — all paths.
		ideChannelManager.ClientConnected += () =>
		{
			logger.LogInformation("IDE client connected — publishing environment state snapshot.");
			_ = PublishStatusAsync(udeiMessage, stoppingToken);
		};

		// Also try immediately (client may already be connected at startup).
		await PublishStatusAsync(udeiMessage, stoppingToken);

		// Keep the service alive for the Host lifetime so the event handler
		// can fire on future reconnections.
		try
		{
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}
		catch (OperationCanceledException)
		{
			// Host shutting down — expected.
		}
	}

	private async Task PublishStatusAsync(DevelopmentEnvironmentStatusIdeMessage message, CancellationToken ct)
	{
		try
		{
			if (await ideChannel.TrySendToIdeAsync(message, ct))
			{
				logger.LogInformation("Published IDE environment status: {Description}", message.Description);
			}
		}
		catch (OperationCanceledException)
		{
			// Shutting down.
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Failed to publish IDE environment status.");
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
