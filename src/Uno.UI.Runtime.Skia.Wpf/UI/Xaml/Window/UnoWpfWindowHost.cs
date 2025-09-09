#nullable enable

using System.Threading;
using System.Windows;
using System.Windows.Markup;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using WpfCanvas = System.Windows.Controls.Canvas;
using WpfContentPresenter = System.Windows.Controls.ContentPresenter;
using WpfControl = System.Windows.Controls.Control;
using WpfWindow = System.Windows.Window;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;
using MUX = Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.UI.Controls;

internal class UnoWpfWindowHost : WpfControl, IWpfWindowHost
{
	private const string NativeOverlayLayerHost = "NativeOverlayLayerHost";
	private const string RenderLayerHost = "RenderLayerHost";
	private readonly Style _style = (Style)XamlReader.Parse(
		"""
		<Style
			xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
			xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
			xmlns:controls="clr-namespace:Uno.UI.Runtime.Skia.Wpf.UI.Controls;assembly=Uno.UI.Runtime.Skia.Wpf"
			TargetType="{x:Type controls:UnoWpfWindowHost}">
			<Setter Property="Template">
				<Setter.Value>
					<ControlTemplate TargetType="{x:Type controls:UnoWpfWindowHost}">
						<Grid>
							<Border
								Background="{x:Null}"
								BorderBrush="{TemplateBinding BorderBrush}"
								BorderThickness="{TemplateBinding BorderThickness}">
								<ContentPresenter x:Name="RenderLayerHost" />
							</Border>
							<Border
								Background="{x:Null}"
								BorderBrush="{TemplateBinding BorderBrush}"
								BorderThickness="{TemplateBinding BorderThickness}">
								<ContentPresenter x:Name="NativeOverlayLayerHost" />
							</Border>
						</Grid>
					</ControlTemplate>
				</Setter.Value>
			</Setter>
		</Style>
		""");

	private readonly UnoWpfWindow _wpfWindow;
	private readonly MUX.Window _winUIWindow;

	private readonly WpfCanvas _nativeOverlayLayer;
	private readonly RenderingLayerHost _renderLayer;

	private readonly SerialDisposable _backgroundDisposable = new();
	private bool _enqueued;

	public UnoWpfWindowHost(UnoWpfWindow wpfWindow, MUX.Window winUIWindow)
	{
		_wpfWindow = wpfWindow;
		_winUIWindow = winUIWindow;

		Style = _style;

		// We need to set the content here because the RenderingLayerHost needs to call Window.GetWindow
		wpfWindow.Content = this;

		FocusVisualStyle = null;

		_nativeOverlayLayer = new WpfCanvas();
		// Transparency doesn't work with the OpenGL renderer, so we have to use the software renderer for the top layer
		_renderLayer = new RenderingLayerHost(WpfRendererProvider.CreateForHost(this));

		Loaded += WpfHost_Loaded;
		Unloaded += (_, _) => _backgroundDisposable.Dispose();

		UpdateRendererBackground();
		_backgroundDisposable.Disposable = _winUIWindow.RegisterBackgroundChangedEvent((_, _) => UpdateRendererBackground());
	}

	public WpfWindow Window => _wpfWindow;

	public WpfControl RenderLayer => _renderLayer;
	public WpfControl BottomLayer => _renderLayer;

	internal void InitializeRenderer()
	{
		_renderLayer.Renderer = WpfRendererProvider.CreateForHost(this);
		UpdateRendererBackground();
	}

	private void WpfHost_Loaded(object _, System.Windows.RoutedEventArgs __)
	{
		// Avoid dotted border on focus.
		if (Parent is WpfControl control)
		{
			control.FocusVisualStyle = null;
			_renderLayer.FocusVisualStyle = null;
		}
	}

	void UpdateRendererBackground()
	{
		if (_renderLayer.Renderer is null)
		{
			return;
		}

		// the flyout layer always has a transparent background so that elements underneath can be seen.
		_renderLayer.Renderer.BackgroundColor = SKColors.Transparent;

		if (_winUIWindow.Background is MUX.Media.SolidColorBrush brush)
		{
			_wpfWindow.Background = new System.Windows.Media.SolidColorBrush(brush.Color.ToWpfColor());
		}
		else if (_winUIWindow.Background is not null)
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

		if (GetTemplateChild(NativeOverlayLayerHost) is WpfContentPresenter nativeOverlayLayerHost)
		{
			nativeOverlayLayerHost.Content = _nativeOverlayLayer;
		}
		if (GetTemplateChild(RenderLayerHost) is WpfContentPresenter renderLayerHost)
		{
			renderLayerHost.Content = _renderLayer;
		}
	}

	MUX.UIElement? IXamlRootHost.RootElement => _winUIWindow.RootElement;

	void IXamlRootHost.InvalidateRender()
	{
		if (!Interlocked.Exchange(ref _enqueued, true))
		{
			// We schedule on Idle here because if you invalidate directly, it can cause a hang if
			// the rendering is continuously invalidated (e.g. animations). Try Given_SKCanvasElement to confirm.
			NativeDispatcher.Main.Enqueue(() =>
			{
				Interlocked.Exchange(ref _enqueued, false);
				_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();
				_renderLayer.InvalidateVisual();
				InvalidateVisual();
			}, NativeDispatcherPriority.Idle);
		}
	}

	WpfCanvas IWpfXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	bool IWpfXamlRootHost.IgnorePixelScaling => WpfHost.Current?.IgnorePixelScaling ?? false;

	RenderSurfaceType? IWpfXamlRootHost.RenderSurfaceType => WpfHost.Current?.RenderSurfaceType ?? null;

	private class RenderingLayerHost(IWpfRenderer renderer) : WpfControl
	{
		public IWpfRenderer? Renderer { get; set; } = renderer;

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			Renderer?.Render(drawingContext);
		}
	}
}
