#nullable enable

using System;
using System.IO;
using System.Windows;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using Uno.UI.Skia.Platform;
using Uno.UI.Xaml.Core;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;
using Windows.UI.ViewManagement;
using UnoApplication = Windows.UI.Xaml.Application;
using WinUI = Windows.UI.Xaml;
using WpfApplication = System.Windows.Application;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Skia.Wpf;

[TemplatePart(Name = NativeOverlayLayerPart, Type = typeof(WpfCanvas))]
internal class UnoWpfWindow : WpfWindow
{
	private const string NativeOverlayLayerPart = "NativeOverlayLayer";

	private readonly WinUI.Window _window;
	private WpfCanvas? _nativeOverlayLayer;

	static UnoWpfWindow()
	{
		DefaultStyleKeyProperty.OverrideMetadata(
			typeof(UnoWpfWindow),
			new WpfFrameworkPropertyMetadata(typeof(WpfHost)));
	}

	public UnoWpfWindow(WinUI.Window window)
	{
		FocusVisualStyle = null;

		SizeChanged += WpfHost_SizeChanged;
		Loaded += WpfHost_Loaded;

		Windows.Foundation.Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Windows.Foundation.Size.Empty)
		{
			WpfApplication.Current.MainWindow.Width = (int)preferredWindowSize.Width;
			WpfApplication.Current.MainWindow.Height = (int)preferredWindowSize.Height;
		}

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

		RegisterForBackgroundColor();
		_window = window;
	}

	internal WpfCanvas? NativeOverlayLayer => _nativeOverlayLayer;

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_nativeOverlayLayer = GetTemplateChild(NativeOverlayLayerPart) as WpfCanvas;

		// App needs to be created after the native overlay layer is properly initialized
		// otherwise the initially focused input element would cause exception.
		StartApp();
	}

	private void WpfHost_Loaded(object sender, RoutedEventArgs e)
	{
		if (_renderer is not null && !_renderer.Initialize())
		{
			// OpenGL initialization failed, fallback to software rendering
			// This may happen on headless systems or containers.

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"OpenGL failed to initialize, using software rendering");
			}

			RenderSurfaceType = Skia.RenderSurfaceType.Software;
			InitializeRenderer();

			_renderer.Initialize();
		}

		WinUI.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(ActualWidth, ActualHeight));

		// Avoid dotted border on focus.
		if (Parent is WpfControl control)
		{
			control.FocusVisualStyle = null;
		}
	}

	private void OnCoreWindowContentRootSet(object? sender, object e)
	{
		var xamlRoot = CoreServices.Instance
			.ContentRootCoordinator
			.CoreWindowContentRoot?
			.GetOrCreateXamlRoot();

		if (xamlRoot is null)
		{
			throw new InvalidOperationException("XamlRoot was not properly initialized");
		}

		XamlRootMap.Register(xamlRoot, this);

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
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

				WpfApplication.Current.MainWindow.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
			}
			else if (Windows.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				WpfApplication.Current.MainWindow.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(scaledPath));
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
			}
		}

		Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
	}

	private void WpfHost_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
	{
		WinUI.Window.Current.OnNativeSizeChanged(
			new Windows.Foundation.Size(
				e.NewSize.Width,
				e.NewSize.Height
			)
		);
	}

	private void RegisterForBackgroundColor()
	{
		void Update()
		{
			if (WinUI.Window.Current.Background is WinUI.Media.SolidColorBrush brush)
			{
				if (_renderer is not null)
				{
					_renderer.BackgroundColor = brush.Color;
				}
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"This platform only supports SolidColorBrush for the Window background");
				}
			}
		}

		Update();

		_registrations.Add(WinUI.Window.Current.RegisterBackgroundChangedEvent((s, e) => Update()));
	}
}
