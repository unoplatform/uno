#nullable enable

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.Skia.Platform;
using Uno.UI.Xaml.Core;
using Uno.UI.XamlHost.Skia.Wpf;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Input;
using UnoApplication = Windows.UI.Xaml.Application;
using WinUI = Windows.UI.Xaml;
using WpfApplication = System.Windows.Application;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using WpfWindow = System.Windows.Window;

namespace Uno.UI.Skia.Wpf;

[TemplatePart(Name = NativeOverlayLayerPart, Type = typeof(WpfCanvas))]
internal class UnoWpfWindow : WpfWindow, IWpfWindowHost
{
	private const string NativeOverlayLayerPart = "NativeOverlayLayer";

	private readonly WinUI.Window _window;
	private readonly CompositeDisposable _disposables = new();
	private readonly HostPointerHandler? _hostPointerHandler;

	private WpfCanvas? _nativeOverlayLayer;
	private IWpfRenderer? _renderer;
	private FocusManager? _focusManager;
	private bool _rendererInitialized;

	static UnoWpfWindow()
	{
		DefaultStyleKeyProperty.OverrideMetadata(
			typeof(UnoWpfWindow),
			new WpfFrameworkPropertyMetadata(typeof(UnoWpfWindow)));
	}

	public UnoWpfWindow(WinUI.Window window)
	{
		_window = window;
		_window.Shown += OnShown;
		_hostPointerHandler = new HostPointerHandler(this);

		FocusVisualStyle = null;

		SizeChanged += WpfHost_SizeChanged;
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

	private void OnShown(object? sender, EventArgs e) => Show();

	WinUI.UIElement? IWpfXamlRootHost.RootElement => _window.RootElement;

	WpfCanvas? IWpfXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	WinUI.XamlRoot? IWpfXamlRootHost.XamlRoot => _window.RootElement?.XamlRoot;

	public bool IgnorePixelScaling => WpfHost.Current!.IgnorePixelScaling;

	public bool IsIsland => false;

	void IWpfXamlRootHost.InvalidateRender()
	{
		InvalidateOverlays();
		InvalidateVisual();
	}

	void IWpfXamlRootHost.ReleasePointerCapture() => ReleaseMouseCapture(); //TODO: This should capture the correct type of pointer (stylus/mouse/touch) https://github.com/unoplatform/uno/issues/8978[capture]

	void IWpfXamlRootHost.SetPointerCapture() => CaptureMouse();

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		_nativeOverlayLayer = GetTemplateChild(NativeOverlayLayerPart) as WpfCanvas;
	}

	private void WpfHost_Loaded(object sender, RoutedEventArgs e)
	{
		WinUI.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(ActualWidth, ActualHeight));

		// Avoid dotted border on focus.
		if (Parent is WpfControl control)
		{
			control.FocusVisualStyle = null;
		}
	}

	private void InitializeRenderer()
	{
		// TODO:MZ: Do this only once, not for every window
		if (WpfHost.Current!.RenderSurfaceType is null)
		{
			WpfHost.Current!.RenderSurfaceType = Skia.RenderSurfaceType.OpenGL;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Using {WpfHost.Current!.RenderSurfaceType} rendering");
		}

		_renderer = WpfHost.Current!.RenderSurfaceType switch
		{
			Skia.RenderSurfaceType.Software => new SoftwareWpfRenderer(this),
			Skia.RenderSurfaceType.OpenGL => new OpenGLWpfRenderer(this),
			_ => throw new InvalidOperationException($"Render Surface type {WpfHost.Current!.RenderSurfaceType} is not supported")
		};

		UpdateRendererBackground();

		if (!_renderer.Initialize())
		{
			// OpenGL initialization failed, fallback to software rendering
			// This may happen on headless systems or containers.

			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"OpenGL failed to initialize, using software rendering");
			}

			WpfHost.Current!.RenderSurfaceType = Skia.RenderSurfaceType.Software;
			InitializeRenderer();
		}
		else
		{
			_rendererInitialized = true;
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

				Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
			}
			else if (Windows.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
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

		Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
	}

	private void WpfHost_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
	{
		// TODO:MZ: Use Content.Size!
		WinUI.Window.Current.OnNativeSizeChanged(
			new Windows.Foundation.Size(
				e.NewSize.Width,
				e.NewSize.Height
			)
		);
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

		if (!_rendererInitialized)
		{
			InitializeRenderer();
		}
		_renderer?.Render(drawingContext);
	}

	private void InvalidateOverlays()
	{
		_focusManager ??= VisualTree.GetFocusManagerForElement(_window.RootElement);
		_focusManager?.FocusRectManager?.RedrawFocusVisual();
		if (_focusManager?.FocusedElement is Windows.UI.Xaml.Controls.TextBox textBox)
		{
			textBox.TextBoxView?.Extension?.InvalidateLayout();
		}
	}
}
