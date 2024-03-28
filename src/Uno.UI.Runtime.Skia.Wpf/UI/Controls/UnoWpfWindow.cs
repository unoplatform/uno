#nullable enable

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.IO;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.UI.ViewManagement;
using WinUI = Microsoft.UI.Xaml;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class UnoWpfWindow : WpfWindow
{
	private readonly ApplicationView _applicationView;

	private bool _shown;

	private static readonly ConcurrentDictionary<WinUI.Window, WpfWindow> _windowToWpfWindow = new();

	public UnoWpfWindow(WinUI.Window winUIWindow, WinUI.XamlRoot xamlRoot)
	{
		_windowToWpfWindow[winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow))] = this;
		winUIWindow.Closed += (_, _) => _windowToWpfWindow.TryRemove(winUIWindow, out _);

		Windows.Foundation.Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Windows.Foundation.Size.Empty)
		{
			Width = (int)preferredWindowSize.Width;
			Height = (int)preferredWindowSize.Height;
		}

		Content = Host = new UnoWpfWindowHost(this, winUIWindow);
		WpfManager.XamlRootMap.Register(xamlRoot, Host);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;
		Closed += UnoWpfWindow_Closed;
		Activated += UnoWpfWindow_Activated;

		UpdateWindowPropertiesFromPackage();
		UpdateWindowPropertiesFromApplicationView();
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
	}

	internal UnoWpfWindowHost Host { get; private set; }

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	internal void UpdateWindowPropertiesFromApplicationView()
	{
		MinWidth = _applicationView.PreferredMinSize.Width;
		MinHeight = _applicationView.PreferredMinSize.Height;
	}

	internal void UpdateWindowPropertiesFromPackage()
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
}
