#nullable enable

using System;
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
	private readonly WinUI.Window _winUIWindow;
	private readonly ApplicationView _applicationView;

	public UnoWpfWindow(WinUI.Window winUIWindow, XamlRoot xamlRoot)
	{
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));

		Windows.Foundation.Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Windows.Foundation.Size.Empty)
		{
			Width = (int)preferredWindowSize.Width;
			Height = (int)preferredWindowSize.Height;
		}

		Content = Host = new UnoWpfWindowHost(this, winUIWindow);
		WpfManager.XamlRootMap.Register(xamlRoot, Host);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged; //TODO:MZ: Unsubscribe
	}

	internal UnoWpfWindowHost Host { get; private set; }

	//TODO:MZ: Call this?
	private void OnCoreWindowContentRootSet(object? sender, object e)
	{
		var contentRoot = CoreServices.Instance
				.ContentRootCoordinator
				.CoreWindowContentRoot;

		var xamlRoot = contentRoot?.GetOrCreateXamlRoot();

		if (xamlRoot is null)
		{
			throw new InvalidOperationException("XamlRoot was not properly initialized");
		}

		contentRoot!.SetHost(this);
		WpfManager.XamlRootMap.Register(xamlRoot, Host);

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	internal void UpdateWindowPropertiesFromApplicationView()
	{
		Title = _applicationView.Title;
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

		if (string.IsNullOrEmpty(_applicationView.Title))
		{
			_applicationView.Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}
	}
}
