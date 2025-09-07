using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.IDE;

internal static class UnoDevelopmentEnvironmentIndicatorExtensions
{
	/// <summary>
	/// Indicates dev-server is starting successfully.
	/// </summary>
	public static async ValueTask NotifyDevServerStartingAsync(this IUnoDevelopmentEnvironmentIndicator udei, CancellationToken ct)
		=> await udei.NotifyAsync(new DevelopmentEnvironmentStatusIdeMessage(DevelopmentEnvironmentComponent.DevServer, DevelopmentEnvironmentStatus.Initializing, "Starting", null, null, [Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"))]), ct);

	/// <summary>
	/// Indicates dev-server process has been killed (and will not restart by its own).
	/// </summary>
	public static async ValueTask NotifyDevServerKilledAsync(this IUnoDevelopmentEnvironmentIndicator udei, CancellationToken ct)
		=> await udei.NotifyAsync(new DevelopmentEnvironmentStatusIdeMessage(DevelopmentEnvironmentComponent.DevServer, DevelopmentEnvironmentStatus.Error, "Not running", null, null, [Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"))]), ct);

	/// <summary>
	/// Indicates dev-server is restarting.
	/// </summary>
	public static async ValueTask NotifyDevServerRestartAsync(this IUnoDevelopmentEnvironmentIndicator udei, CancellationToken ct)
		=> await udei.NotifyAsync(new DevelopmentEnvironmentStatusIdeMessage(DevelopmentEnvironmentComponent.DevServer, DevelopmentEnvironmentStatus.Warning, "Restarting", null, null, [Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"))]), ct);

	/// <summary>
	/// Indicates dev-server is restarting.
	/// </summary>
	public static async ValueTask NotifyDevServerTimeoutAsync(this IUnoDevelopmentEnvironmentIndicator udei, CancellationToken ct)
		=> await udei.NotifyAsync(new DevelopmentEnvironmentStatusIdeMessage(DevelopmentEnvironmentComponent.DevServer, DevelopmentEnvironmentStatus.Error, "Timeout", "Dev-server didn't connected back to IDE in the given delay", null, [Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"))]), ct);
}
