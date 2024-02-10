#nullable enable

using System;
using System.ComponentModel;
using System.IO;

using Uno.Foundation.Logging;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using IOPath = System.IO.Path;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowNative
{
	private readonly WinUIWindow _winUIWindow;
	private readonly bool _mainWindow;
	private readonly nint _handle;
	private readonly ApplicationView _applicationView;

	public MacOSWindowNative(WinUIWindow winUIWindow, Microsoft.UI.Xaml.XamlRoot xamlRoot)
	{
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));

		var defaultWidth = 1024.0;
		var defaultHeight = 800.0;
		Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Size.Empty)
		{
			defaultWidth = preferredWindowSize.Width;
			defaultHeight = preferredWindowSize.Height;
		}

		if (MacSkiaHost.Current is not null)
		{
			_mainWindow = true;
			_handle = NativeUno.uno_app_get_main_window();
			MacSkiaHost.Current.InitialWindow ??= this;
		}
		else
		{
			_mainWindow = false;
			_handle = NativeUno.uno_window_create(defaultWidth, defaultHeight);
		}

		// host MUST be created after we get a native handle
		Host = new MacOSWindowHost(this, winUIWindow);

		// TODO: combine calls
		MacOSWindowHost.Register(_handle, Host);
		MacOSWindowHost.XamlRootMap.Register(xamlRoot, Host);

		// call resize as late as possible (after the host creation)
		NativeUno.uno_window_resize(_handle, defaultWidth, defaultHeight);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;

		UpdateWindowPropertiesFromPackage();
		UpdateWindowPropertiesFromApplicationView();
	}

	internal MacOSWindowHost Host { get; }
	internal nint Handle => _handle;

	internal bool MainWindow => _mainWindow;

	// FIXME: should be shared with GTK and X11 hosts with a delegate to set the icon from a filename
	internal void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
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

				NativeUno.uno_application_set_icon(iconPath);
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

	internal void UpdateWindowPropertiesFromApplicationView()
	{
		NativeUno.uno_window_set_title(_handle, _applicationView.Title);
		var minSize = _applicationView.PreferredMinSize;
		NativeUno.uno_window_set_min_size(_handle, minSize.Width, minSize.Height);
	}
}
