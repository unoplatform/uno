#nullable enable

using Uno.UI.Runtime.Skia.Wpf.Hosting;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.Xaml.Core;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;
using Windows.UI.ViewManagement;
using WinUI = Windows.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfContentPresenter = System.Windows.Controls.ContentPresenter;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using WpfWindow = System.Windows.Window;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Uno.UI.Hosting;
using Uno.UI.Rendering;

namespace Uno.UI.Skia.Wpf;

[TemplatePart(Name = NativeOverlayLayerHostPart, Type = typeof(WpfCanvas))]
internal class UnoWpfWindowHost : WpfControl, IWpfWindowHost
{
	private const string NativeOverlayLayerHostPart = "NativeOverlayLayerHost";

	private readonly WpfCanvas _nativeOverlayLayer = new();
	private readonly WpfWindow _wpfWindow;
	private readonly WinUI.Window _window;
	private readonly CompositeDisposable _disposables = new();

	private Size _previousArrangeBounds;

	private IWpfRenderer? _renderer;

	static UnoWpfWindowHost()
	{
		DefaultStyleKeyProperty.OverrideMetadata(
			typeof(UnoWpfWindowHost),
			new WpfFrameworkPropertyMetadata(typeof(UnoWpfWindowHost)));
	}

	public UnoWpfWindowHost(WpfWindow wpfWindow, WinUI.Window window)
	{
		_wpfWindow = wpfWindow;
		_window = window;

		FocusVisualStyle = null;

		Loaded += WpfHost_Loaded;

		Windows.Foundation.Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Windows.Foundation.Size.Empty)
		{
			Width = (int)preferredWindowSize.Width;
			Height = (int)preferredWindowSize.Height;
		}

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

		RegisterForBackgroundColor();
		UpdateWindowPropertiesFromPackage();
	}

	protected override Size ArrangeOverride(Size arrangeBounds)
	{
		if (arrangeBounds != _previousArrangeBounds)
		{
			_window.OnNativeSizeChanged(new Windows.Foundation.Size(arrangeBounds.Width, arrangeBounds.Height));
			_previousArrangeBounds = arrangeBounds;
		}
		return base.ArrangeOverride(arrangeBounds);
	}

	WinUI.UIElement? IXamlRootHost.RootElement => _window.RootElement;

	WpfCanvas? IWpfXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	WinUI.XamlRoot? IXamlRootHost.XamlRoot => _window.RootElement?.XamlRoot;

	bool IWpfXamlRootHost.IgnorePixelScaling => WpfHost.Current!.IgnorePixelScaling;

	RenderSurfaceType? IWpfXamlRootHost.RenderSurfaceType => WpfHost.Current!.RenderSurfaceType;

	public bool IsIsland => false;

	void IXamlRootHost.InvalidateRender()
	{
		_window.RootElement?.XamlRoot?.InvalidateOverlays();
		InvalidateVisual();
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (GetTemplateChild(NativeOverlayLayerHostPart) is WpfContentPresenter nativeOverlayLayerHost)
		{
			nativeOverlayLayerHost.Content = _nativeOverlayLayer;
		}
	}

	private void WpfHost_Loaded(object sender, RoutedEventArgs e)
	{
		// Avoid dotted border on focus.
		if (Parent is WpfControl control)
		{
			control.FocusVisualStyle = null;
		}
	}

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

				_wpfWindow.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
			}
			else if (Windows.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				_wpfWindow.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(scaledPath));
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

	private void RegisterForBackgroundColor()
	{
		UpdateRendererBackground();

		_disposables.Add(_window.RegisterBackgroundChangedEvent((s, e) => UpdateRendererBackground()));
	}

	private void UpdateRendererBackground()
	{
		if (_window.Background is WinUI.Media.SolidColorBrush brush)
		{
			if (_renderer is not null)
			{
				_renderer.BackgroundColor = brush.Color;
				Background = new SolidColorBrush(brush.Color.ToWpfColor());
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

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);

		if (_renderer is null)
		{
			_renderer = WpfRendererProvider.CreateForHost(this);
			UpdateRendererBackground();
		}

		_renderer?.Render(drawingContext);
	}
}
