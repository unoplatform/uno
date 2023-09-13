#nullable enable

using System.Windows;
using System.Windows.Media;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using Uno.UI.Xaml.Core;
using WinUI = Microsoft.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfContentPresenter = System.Windows.Controls.ContentPresenter;
using WpfControl = System.Windows.Controls.Control;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

[TemplatePart(Name = NativeOverlayLayerHostPart, Type = typeof(WpfCanvas))]
internal class UnoWpfWindowHost : WpfControl, IWpfWindowHost
{
	private const string NativeOverlayLayerHostPart = "NativeOverlayLayerHost";

	private readonly WpfCanvas _nativeOverlayLayer = new();
	private readonly UnoWpfWindow _wpfWindow;
	private readonly WinUI.Window _winUIWindow;
	private readonly CompositeDisposable _disposables = new();

	private IWpfRenderer? _renderer;

	static UnoWpfWindowHost()
	{
		DefaultStyleKeyProperty.OverrideMetadata(
			typeof(UnoWpfWindowHost),
			new WpfFrameworkPropertyMetadata(typeof(UnoWpfWindowHost)));
	}

	public UnoWpfWindowHost(UnoWpfWindow wpfWindow, WinUI.Window winUIWindow)
	{
		_wpfWindow = wpfWindow;
		_winUIWindow = winUIWindow;

		FocusVisualStyle = null;

		Loaded += WpfHost_Loaded;

		RegisterForBackgroundColor();
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
