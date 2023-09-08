#nullable enable

using Uno.UI.Runtime.Skia.Wpf.Hosting;
using System;
using System.Windows;
using System.Windows.Media;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.Xaml.Core;
using WinUI = Microsoft.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfControl = System.Windows.Controls.Control;
using WpfContentPresenter = System.Windows.Controls.ContentPresenter;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using WpfWindow = System.Windows.Window;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Uno.UI.Hosting;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

[TemplatePart(Name = NativeOverlayLayerHostPart, Type = typeof(WpfCanvas))]
internal class UnoWpfWindowHost : WpfControl, IWpfWindowHost
{
	private const string NativeOverlayLayerHostPart = "NativeOverlayLayerHost";

	private readonly WpfCanvas _nativeOverlayLayer = new();
	private readonly WpfWindow _wpfWindow;
	private readonly WinUI.Window _winUIWindow;
	private readonly CompositeDisposable _disposables = new();

	private Size _previousArrangeBounds;

	private IWpfRenderer? _renderer;

	static UnoWpfWindowHost()
	{
		DefaultStyleKeyProperty.OverrideMetadata(
			typeof(UnoWpfWindowHost),
			new WpfFrameworkPropertyMetadata(typeof(UnoWpfWindowHost)));
	}

	public UnoWpfWindowHost(WpfWindow wpfWindow, WinUI.Window winUIWindow)
	{
		_wpfWindow = wpfWindow;
		_winUIWindow = winUIWindow;

		FocusVisualStyle = null;

		Loaded += WpfHost_Loaded;

		RegisterForBackgroundColor();
	}

	protected override Size ArrangeOverride(Size arrangeBounds)
	{
		if (arrangeBounds != _previousArrangeBounds)
		{
			_winUIWindow.OnNativeSizeChanged(new Windows.Foundation.Size(arrangeBounds.Width, arrangeBounds.Height));
			_previousArrangeBounds = arrangeBounds;
		}
		return base.ArrangeOverride(arrangeBounds);
	}

	WinUI.UIElement? IXamlRootHost.RootElement => _winUIWindow.RootElement;

	WpfCanvas? IWpfXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	bool IWpfXamlRootHost.IgnorePixelScaling => WpfHost.Current!.IgnorePixelScaling;

	RenderSurfaceType? IWpfXamlRootHost.RenderSurfaceType => WpfHost.Current!.RenderSurfaceType;

	public bool IsIsland => false;

	void IXamlRootHost.InvalidateRender()
	{
		_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();
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

	private void RegisterForBackgroundColor()
	{
		UpdateRendererBackground();

		_disposables.Add(_winUIWindow.RegisterBackgroundChangedEvent((s, e) => UpdateRendererBackground()));
	}

	private void UpdateRendererBackground()
	{
		if (_winUIWindow.Background is WinUI.Media.SolidColorBrush brush)
		{
			if (_renderer is not null)
			{
				_renderer.BackgroundColor = brush.Color;
				_wpfWindow.Background = new SolidColorBrush(brush.Color.ToWpfColor());
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
