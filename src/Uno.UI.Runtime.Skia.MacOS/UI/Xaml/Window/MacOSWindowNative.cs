using System.ComponentModel;

using Uno.Foundation.Logging;

using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.UI.Xaml.Controls;
using IOPath = System.IO.Path;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowNative
{
	private readonly WinUIWindow _winUIWindow;
	private readonly ApplicationView _applicationView;

	public MacOSWindowNative(WinUIWindow winUIWindow, Microsoft.UI.Xaml.XamlRoot xamlRoot, out Size initialSize)
	{
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));

		double initialWidth = NativeWindowWrapperBase.InitialWidth;
		double initialHeight = NativeWindowWrapperBase.InitialHeight;
		var preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Size.Empty)
		{
			initialWidth = preferredWindowSize.Width;
			initialHeight = preferredWindowSize.Height;
		}

		initialSize = new Size(initialWidth, initialHeight);

		if (MacSkiaHost.Current.InitialWindow is null)
		{
			// the first window was already created much earlier in native code
			Handle = NativeUno.uno_app_get_main_window();
			MacSkiaHost.Current.InitialWindow = this;
		}
		else
		{
			Handle = NativeUno.uno_window_create(initialWidth, initialHeight);
		}

		// host MUST be created after we get a native handle
		Host = new MacOSWindowHost(this, winUIWindow, xamlRoot);

		MacOSWindowHost.Register(Handle, xamlRoot, Host);

		// call resize as late as possible (after the host creation)
		NativeUno.uno_window_resize(Handle, initialWidth, initialHeight);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);

		NativeWindowReady?.Invoke(this, this);

		UpdateWindowPropertiesFromPackage();
		UpdateWindowPropertiesFromApplicationView();
	}

	internal MacOSWindowHost Host { get; }
	internal nint Handle { get; private set; }

	internal static event EventHandler<MacOSWindowNative>? NativeWindowReady;

	internal void Destroyed()
	{
		Handle = nint.Zero;
	}

	// FIXME: should be shared with GTK and X11 hosts with a delegate to set the icon from a filename
	private void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is { } uri)
		{
			var basePath = uri.OriginalString.Replace('\\', IOPath.DirectorySeparatorChar);
			var iconPath = IOPath.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				NativeUno.uno_application_set_icon(iconPath);
			}
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				NativeUno.uno_application_set_icon(scaledPath);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
			}
		}

		if (string.IsNullOrEmpty(_applicationView.Title))
		{
			_applicationView.Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	private void UpdateWindowPropertiesFromApplicationView()
	{
		NativeUno.uno_window_set_title(Handle, _applicationView.Title);
	}
}
