#nullable enable

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using Gtk;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Gtk.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using IOPath = System.IO.Path;
using WinUIApplication = Windows.UI.Xaml.Application;
using WinUIWindow = Windows.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class UnoGtkWindow : Window
{
	private readonly WinUIWindow _winUIWindow;
	private readonly ApplicationView _applicationView;
	private static readonly ConcurrentDictionary<WinUIWindow, UnoGtkWindow> _windowToGtkWindow = new();

	public static UnoGtkWindow? GetGtkWindowFromWindow(WinUIWindow window)
		=> _windowToGtkWindow.TryGetValue(window, out var gtkWindow) ? gtkWindow : null;

	public UnoGtkWindow(WinUIWindow winUIWindow, Windows.UI.Xaml.XamlRoot xamlRoot) : base(WindowType.Toplevel)
	{
		_winUIWindow = winUIWindow;
		_windowToGtkWindow[winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow))] = this;
		winUIWindow.Closed += (_, _) => _windowToGtkWindow.TryRemove(winUIWindow, out _);

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

		if (GtkHost.Current is not null)
		{
			GtkHost.Current.InitialWindow ??= this;
		}

		GtkManager.XamlRootMap.Register(xamlRoot, Host);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;
		winUIWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged += ExtendContentIntoTitleBar;
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += UpdateWindowPropertiesFromCoreApplication;
		Destroyed += UnoGtkWindow_Destroyed;
		Shown += UnoGtkWindow_Shown;
		UpdateWindowPropertiesFromPackage();
		UpdateWindowPropertiesFromApplicationView();
		UpdateWindowPropertiesFromCoreApplication();
	}

	internal static event EventHandler<UnoGtkWindow>? NativeWindowShown;

	private void UnoGtkWindow_Shown(object? sender, EventArgs e) => NativeWindowShown?.Invoke(this, this);

	private void UnoGtkWindow_Destroyed(object? sender, EventArgs e)
	{
		_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged -= UpdateWindowPropertiesFromCoreApplication;
		_winUIWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged -= ExtendContentIntoTitleBar;
	}

	internal UnoGtkWindowHost Host { get; }

	private void UpdateWindowPropertiesFromPackage()
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
			else if (Windows.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
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

		if (!string.IsNullOrEmpty(Windows.ApplicationModel.Package.Current.DisplayName))
		{
			Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	private void UpdateWindowPropertiesFromApplicationView()
	{
		SetSizeRequest((int)_applicationView.PreferredMinSize.Width, (int)_applicationView.PreferredMinSize.Height);
	}

	private void UpdateWindowPropertiesFromCoreApplication()
	{
		var coreApplicationView = CoreApplication.GetCurrentView();
		ExtendContentIntoTitleBar(coreApplicationView.TitleBar.ExtendViewIntoTitleBar);
	}

	internal void ExtendContentIntoTitleBar(bool extend) => Decorated = !extend;
}
