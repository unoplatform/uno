#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using WinUI = Microsoft.UI.Xaml;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class UnoWpfWindow : WpfWindow
{
	private readonly WinUI.Window _winUIWindow;
	private IWpfWindowHost _host;
	private bool _isVisible;

	public UnoWpfWindow(WinUI.Window winUIWindow, XamlRoot xamlRoot)
	{
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));
		_winUIWindow.Showing += OnShowing;
		_winUIWindow.NativeWindow = this;

		Windows.Foundation.Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Windows.Foundation.Size.Empty)
		{
			Width = (int)preferredWindowSize.Width;
			Height = (int)preferredWindowSize.Height;
		}

		Content = _host = new UnoWpfWindowHost(this, winUIWindow);
		WpfManager.XamlRootMap.Register(xamlRoot, _host);

		Closing += OnClosing;
		Activated += OnActivated;
		Deactivated += OnDeactivated;
		StateChanged += OnStateChanged;

		ApplicationView.GetForCurrentView().PropertyChanged += OnApplicationViewPropertyChanged;

		winUIWindow.OnNativeWindowCreated();
	}

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
		WpfManager.XamlRootMap.Register(xamlRoot, _host);

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	}

	private void OnShowing(object? sender, EventArgs e) => Show();

	private void OnClosing(object? sender, CancelEventArgs e)
	{
		// TODO: Support multi-window approach properly #8341
		var manager = SystemNavigationManagerPreview.GetForCurrentView();
		if (!manager.HasConfirmedClose)
		{
			if (!manager.RequestAppClose())
			{
				e.Cancel = true;
				return;
			}
		}

		// Closing should continue, perform suspension.
		WinUIApplication.Current.RaiseSuspending();
	}

	private void OnDeactivated(object? sender, EventArgs e) =>
		_winUIWindow?.OnNativeActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);

	private void OnActivated(object? sender, EventArgs e)
	{
		_winUIWindow.OnNativeActivated(Windows.UI.Core.CoreWindowActivationState.PointerActivated);
		_host.Focus();
	}

	private void OnStateChanged(object? sender, EventArgs e)
	{
		var application = WinUIApplication.Current;
		var wasVisible = _isVisible;

		_isVisible = WindowState != System.Windows.WindowState.Minimized;

		if (wasVisible && !_isVisible)
		{
			_winUIWindow.OnNativeVisibilityChanged(false);
			application?.RaiseEnteredBackground(null);
		}
		else if (!wasVisible && _isVisible)
		{
			application?.RaiseLeavingBackground(() => _winUIWindow?.OnNativeVisibilityChanged(true));
		}
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	internal void UpdateWindowPropertiesFromApplicationView()
	{
		var appView = ApplicationView.GetForCurrentView();
		Title = appView.Title;
		MinWidth = appView.PreferredMinSize.Width;
		MinHeight = appView.PreferredMinSize.Height;
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

		if (string.IsNullOrEmpty(ApplicationView.GetForCurrentView().Title))
		{
			ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
		}
	}
}
