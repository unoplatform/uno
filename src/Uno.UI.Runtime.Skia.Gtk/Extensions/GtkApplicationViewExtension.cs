#nullable enable
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.Runtime.Skia.Gtk;

internal class GtkApplicationViewExtension : IApplicationViewExtension
{
	private readonly ApplicationView _owner;

	public GtkApplicationViewExtension(object owner)
	{
		_owner = (ApplicationView)owner;
	}

	public void ExitFullScreenMode()
	{
		if (GtkHost.Current?.InitialWindow is not { } window)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("There is no main window set.");
			}
			return;
		}

		window.Unfullscreen();
	}

	public bool TryEnterFullScreenMode()
	{
		if (GtkHost.Current?.InitialWindow is not { } window)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("There is no main window set.");
			}
			return false;
		}

		window.Fullscreen();
		return true;
	}

	public bool TryResizeView(Size size)
	{
		if (GtkHost.Current?.InitialWindow is not { } window)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("There is no main window set.");
			}
			return false;
		}

		window.Resize((int)size.Width, (int)size.Height);
		return true;
	}
}
