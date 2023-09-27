#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using Gtk;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Gtk.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using IOPath = System.IO.Path;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class UnoGtkWindow : Window
{
	private readonly WinUIWindow _winUIWindow;
	private readonly ApplicationView _applicationView;

	public UnoGtkWindow(WinUIWindow winUIWindow, XamlRoot xamlRoot) : base(WindowType.Toplevel)
	{
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));

		Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Size.Empty)
		{
			SetDefaultSize((int)preferredWindowSize.Width, (int)preferredWindowSize.Height);
		}
		else
		{
			SetDefaultSize(1024, 800);
		}
		SetPosition(WindowPosition.Center);
		Realized += (s, e) =>
		{
			// Load the correct cursors before the window is shown
			// but after the window has been initialized.
			Cursors.EnsureLoaded();
		};

		Host = new UnoGtkWindowHost(this, winUIWindow);
		if (GtkHost.Current is not null) // TODO:MZ: This is a workaround for DPIHelper, should not be done this way.
		{
			GtkHost.Current.MainWindow = this;
		}
		GtkManager.XamlRootMap.Register(xamlRoot, Host);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged; //TODO:MZ: Unsubscribe
	}

	internal UnoGtkWindowHost Host { get; }

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

				SetIconFromFile(iconPath);
			}
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				SetIconFromFile(scaledPath);
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
		Title = _applicationView.Title;
		SetSizeRequest((int)_applicationView.PreferredMinSize.Width, (int)_applicationView.PreferredMinSize.Height);
	}

	internal void UpdateWindowPropertiesFromCoreApplication()
	{
		var coreApplicationView = CoreApplication.GetCurrentView();
		Decorated = !coreApplicationView.TitleBar.ExtendViewIntoTitleBar;
	}
}
