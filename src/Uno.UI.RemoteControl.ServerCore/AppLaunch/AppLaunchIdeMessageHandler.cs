using System;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.Services;

namespace Uno.UI.RemoteControl.Server.AppLaunch;

/// <summary>
/// Registers the app launches announced by the IDE (<see cref="AppLaunchRegisterIdeMessage"/>) with the <see cref="IApplicationLaunchMonitor"/>.
/// </summary>
/// <remarks>
/// Hosts create a single instance when they start: the IDE announces a launch before the app connects, often while no
/// connection exists, and a per-connection handler would register it once per connection.
/// </remarks>
public sealed class AppLaunchIdeMessageHandler : IDisposable
{
	private readonly IIdeChannel _ideChannel;
	private readonly IApplicationLaunchMonitor _launchMonitor;

	public AppLaunchIdeMessageHandler(IIdeChannel ideChannel, IApplicationLaunchMonitor launchMonitor)
	{
		_ideChannel = ideChannel ?? throw new ArgumentNullException(nameof(ideChannel));
		_launchMonitor = launchMonitor ?? throw new ArgumentNullException(nameof(launchMonitor));

		_ideChannel.MessageFromIde += OnMessageFromIde;
	}

	private void OnMessageFromIde(object? sender, IdeMessage message)
	{
		if (message is not AppLaunchRegisterIdeMessage launch)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().LogDebug("Received app launch register message from IDE: {Msg}", launch);
		}

		_launchMonitor.RegisterLaunch(launch.Mvid, launch.Platform, launch.IsDebug, launch.Ide, launch.Plugin);
	}

	public void Dispose()
		=> _ideChannel.MessageFromIde -= OnMessageFromIde;
}
