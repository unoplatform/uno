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
		DevelopmentEnvironmentStatusIdeMessage udeiMessage;
		if (addIns.Discovery is null or { Error.Length: > 0 } or { AddIns: null }) // Count: 0 is considered as a valid result!
		{
			udeiMessage = new(
				DevelopmentEnvironmentComponent.DevServer,
				DevelopmentEnvironmentStatus.Warning,
				"Failed to discover add-ins",
				addIns.Discovery?.Error,
				null,
				[Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"))]);
		}
		else if (addIns is { Assemblies: null } or { Discovery.AddIns.Count: > 0, Assemblies.Count: 0 }
				|| addIns.Assemblies.Any(result => result.Error is not null))
		{
			udeiMessage = new(
				DevelopmentEnvironmentComponent.DevServer,
				DevelopmentEnvironmentStatus.Warning,
				"Failed to load add-ins",
				addIns.Discovery?.Error,
				null,
				[Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"))]);
		}
		else
		{
			udeiMessage = new(
				DevelopmentEnvironmentComponent.DevServer,
				DevelopmentEnvironmentStatus.Ready,
				"Dev-server is ready",
				null,
				null,
				[Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"))]);
		}

		await ideChannel.SendToIdeAsync(udeiMessage, stoppingToken);
	}
}
