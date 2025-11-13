using Windows.Graphics;
using Uno.Foundation.Logging;
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32ApplicationViewExtension(ApplicationView owner) : IApplicationViewExtension
{
	public bool TryResizeView(Windows.Foundation.Size size)
	{
		if (!AppWindow.TryGetFromWindowId(owner.WindowId, out var appWindow))
		{
			this.LogWarn()?.Warn($"{nameof(AppWindow)} not found for WindowId.");
			return false;
		}

		var window = Window.GetFromAppWindow(appWindow);
		if (window.RootElement?.XamlRoot is not { } xamlRoot)
		{
			this.LogWarn()?.Warn($"The {nameof(XamlRoot)} of the window should have been initialized at this point.");
			return false;
		}

		if (XamlRootMap.GetHostForRoot(xamlRoot) is not Win32WindowWrapper wrapper)
		{
			this.LogWarn()?.Warn($"{nameof(XamlRootMap)} should have been filled at this point.");
			return false;
		}

		wrapper.Resize(new SizeInt32((int)size.Width, (int)size.Height));
		return true;
	}
}
