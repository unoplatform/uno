#nullable enable

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Windows.UI.ViewManagement;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Xaml.Controls;
using WindowChrome = System.Windows.Shell.WindowChrome;
using WinUI = Microsoft.UI.Xaml;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WpfWindow = System.Windows.Window;
using WpfControl = System.Windows.Controls.Control;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class UnoWpfWindow : WpfWindow
{
	private readonly ApplicationView _applicationView;
	private readonly WinUI.Window _winUIWindow;

	private bool _shown;

	private static readonly ConcurrentDictionary<WinUI.Window, WpfWindow> _windowToWpfWindow = new();

	public UnoWpfWindow(WinUI.Window winUIWindow, WinUI.XamlRoot xamlRoot)
	{
		_winUIWindow = winUIWindow;
		_windowToWpfWindow[winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow))] = this;
		winUIWindow.Closed += (_, _) => _windowToWpfWindow.TryRemove(winUIWindow, out _);

		var preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize.IsEmpty)
		{
			preferredWindowSize = new Windows.Foundation.Size(NativeWindowWrapperBase.InitialWidth, NativeWindowWrapperBase.InitialHeight);
		}
		Width = (int)preferredWindowSize.Width;
		Height = (int)preferredWindowSize.Height;

		var windowHost = new UnoWpfWindowHost(this, winUIWindow);
		Host = windowHost;
		XamlRootMap.Register(xamlRoot, (IWpfXamlRootHost)Host);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;
		winUIWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged += ExtendContentIntoTitleBar;
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged += UpdateWindowPropertiesFromCoreApplication;

		Closed += UnoWpfWindow_Closed;
		Activated += UnoWpfWindow_Activated;

		UpdateWindowPropertiesFromPackage();
		UpdateWindowPropertiesFromApplicationView();
		UpdateWindowPropertiesFromCoreApplication();

		this.SourceInitialized += (s, e) => windowHost.InitializeRenderer();
	}

	public static WpfWindow? GetFromWinUIWindow(WinUI.Window window)
		=> _windowToWpfWindow.TryGetValue(window, out var wpfWindow) ? wpfWindow : null;

	private void UnoWpfWindow_Activated(object? sender, EventArgs e)
	{
		Host.Focus();
		if (!_shown)
		{
			_shown = true;
			NativeWindowShown?.Invoke(this, this);
		}
	}

	internal static event EventHandler<UnoWpfWindow>? NativeWindowShown;

	private void UnoWpfWindow_Closed(object? sender, EventArgs e)
	{
		_applicationView.PropertyChanged -= OnApplicationViewPropertyChanged;
		CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBarChanged -= UpdateWindowPropertiesFromCoreApplication;
		_winUIWindow.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged -= ExtendContentIntoTitleBar;
	}

	internal WpfControl Host { get; }

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	private void UpdateWindowPropertiesFromApplicationView()
	{
		MinWidth = _applicationView.PreferredMinSize.Width;
		MinHeight = _applicationView.PreferredMinSize.Height;
	}

	private void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
		{
			var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
			var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
			}
			else if (Microsoft.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(scaledPath));
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

	private void UpdateWindowPropertiesFromCoreApplication()
	{
		var coreApplicationView = CoreApplication.GetCurrentView();
		ExtendContentIntoTitleBar(coreApplicationView.TitleBar.ExtendViewIntoTitleBar);
	}

	internal void ExtendContentIntoTitleBar(bool extend)
	{
		if (extend)
		{
			WindowStyle = WindowStyle.None;
			WindowChrome.SetWindowChrome(this, new WindowChrome
			{
				UseAeroCaptionButtons = false,
				// this removes the thin white bar at the top, but this causes the window to grow a little.
				// No work around has been found for this yet.
				CaptionHeight = 0
			});

			// for some reason touchpad physical presses work without this, but not "taps"
			WindowChrome.SetIsHitTestVisibleInChrome((IInputElement)Content, true);
		}
		else
		{
			WindowStyle = WindowStyle.SingleBorderWindow;
			ClearValue(WindowChrome.WindowChromeProperty);
		}
	}
}
