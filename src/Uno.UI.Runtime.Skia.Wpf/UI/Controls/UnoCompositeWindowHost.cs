#nullable enable

using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfContentPresenter = System.Windows.Controls.ContentPresenter;
using WpfControl = System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using MUX = Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class UnoCompositeWindowHost : WpfControl, IWpfWindowHost
{
	private const string NativeOverlayLayerHost = "NativeOverlayLayerHost";
	private const string BottomLayerHost = "BottomLayerHost";
	private const string FlyoutLayerHost = "FlyoutLayerHost";

	private readonly UnoWpfWindow _wpfWindow;
	private readonly MUX.Window _winUIWindow;

	private readonly RenderingLayerHost _bottomLayer;
	private readonly WpfCanvas _nativeOverlayLayer;
	private readonly RenderingLayerHost _flyoutLayer;

	private readonly SerialDisposable _backgroundDisposable = new();

	static UnoCompositeWindowHost()
	{
		DefaultStyleKeyProperty.OverrideMetadata(
			typeof(UnoCompositeWindowHost),
			new WpfFrameworkPropertyMetadata(typeof(UnoCompositeWindowHost)));
	}

	public UnoCompositeWindowHost(UnoWpfWindow wpfWindow, MUX.Window winUIWindow)
	{
		_wpfWindow = wpfWindow;
		_winUIWindow = winUIWindow;

		// We need to set the content here because the RenderingLayerHost needs to call Window.GetWindow
		wpfWindow.Content = this;

		FocusVisualStyle = null;

		_bottomLayer = new RenderingLayerHost(WpfRendererProvider.CreateForHost(this, false));
		_nativeOverlayLayer = new WpfCanvas();
		_flyoutLayer = new RenderingLayerHost(WpfRendererProvider.CreateForHost(this, true));

		Loaded += WpfHost_Loaded;
		Unloaded += (_, _) => _backgroundDisposable.Dispose();

		UpdateRendererBackground();
		_backgroundDisposable.Disposable = _winUIWindow.RegisterBackgroundChangedEvent((_, _) => UpdateRendererBackground());
	}

	public WpfControl FlyoutLayer => _flyoutLayer;
	public WpfControl BottomLayer => _flyoutLayer;

	private void WpfHost_Loaded(object _, System.Windows.RoutedEventArgs __)
	{
		// Avoid dotted border on focus.
		if (Parent is WpfControl control)
		{
			control.FocusVisualStyle = null;
			_bottomLayer.FocusVisualStyle = null;
			_flyoutLayer.FocusVisualStyle = null;
		}
	}

	void UpdateRendererBackground()
	{
		// the flyout layer always has a transparent background so that elements underneath can be seen.
		_flyoutLayer.Renderer.BackgroundColor = SKColors.Transparent;

		if (_winUIWindow.Background is MUX.Media.SolidColorBrush brush)
		{
			_bottomLayer.Renderer.BackgroundColor = brush.Color;
			_wpfWindow.Background = new System.Windows.Media.SolidColorBrush(brush.Color.ToWpfColor());
		}
		else
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"This platform only supports SolidColorBrush for the Window background");
			}
		}
	}

	public override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		if (GetTemplateChild(BottomLayerHost) is WpfContentPresenter bottomLayerHost)
		{
			bottomLayerHost.Content = _bottomLayer;
		}
		if (GetTemplateChild(NativeOverlayLayerHost) is WpfContentPresenter nativeOverlayLayerHost)
		{
			nativeOverlayLayerHost.Content = _nativeOverlayLayer;
		}
		if (GetTemplateChild(FlyoutLayerHost) is WpfContentPresenter flyoutLayerHost)
		{
			flyoutLayerHost.Content = _flyoutLayer;
		}
	}

	MUX.UIElement? IXamlRootHost.RootElement => _winUIWindow.RootElement;

	void IXamlRootHost.InvalidateRender()
	{
		_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();
		_bottomLayer.InvalidateVisual();
		_flyoutLayer.InvalidateVisual();
		InvalidateVisual();
	}

	WpfCanvas IWpfXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	bool IWpfXamlRootHost.IgnorePixelScaling => WpfHost.Current?.IgnorePixelScaling ?? false;

	RenderSurfaceType? IWpfXamlRootHost.RenderSurfaceType => WpfHost.Current?.RenderSurfaceType ?? null;

	private class RenderingLayerHost(IWpfRenderer renderer) : WpfControl
	{
		public IWpfRenderer Renderer { get; } = renderer;

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			Renderer.Render(drawingContext);
		}
	}
}
