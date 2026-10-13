#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Linux;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Wayland;

// Portals identify a Wayland parent through an xdg-foreign handle: "wayland:<handle>".
internal sealed class WaylandPortalParentWindowProvider(WaylandConnection connection) : IPortalParentWindowProvider
{
	private static readonly TimeSpan _handleTimeout = TimeSpan.FromSeconds(1);

	public async Task<PortalParentWindow?> GetParentWindowAsync(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		if (AppWindow.GetFromWindowId(ApplicationView.GetForCurrentViewSafe().WindowId) is not { } appWindow
			|| Microsoft.UI.Xaml.Window.GetFromAppWindow(appWindow) is not { } window
			|| connection.FindWindow(window) is not { } wrapper)
		{
			return null;
		}

		if (connection.Exporter is not { } exporter)
		{
			// The portal then shows an unparented dialog.
			return new PortalParentWindow(string.Empty);
		}

		var handle = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		ZxdgExportedV2 exported;
		lock (connection.Display.SyncRoot)
		{
			exported = exporter.ExportToplevel(wrapper.Host.Surface);
			exported.HandleEvent += (_, e) => handle.TrySetResult(e.Handle);
		}
		// Destroying the exported object revokes the handle, so it lives until the portal request completes.
		var lifetime = Disposable.Create(() => connection.Display.Post(exported.Dispose));

		try
		{
			var parentHandle = await handle.Task.WaitAsync(_handleTimeout, token);
			return new PortalParentWindow("wayland:" + parentHandle, lifetime);
		}
		catch (TimeoutException)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("The compositor did not provide an xdg-foreign handle in time; the portal dialog will not be parented.");
			}
			return new PortalParentWindow(string.Empty, lifetime);
		}
		catch
		{
			lifetime.Dispose();
			throw;
		}
	}
}
