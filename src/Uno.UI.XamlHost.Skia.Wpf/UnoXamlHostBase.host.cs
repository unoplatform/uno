#nullable enable

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.UI.Content;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.XamlHost.Extensions;
using WinUI = Windows.UI.Xaml;
using WpfCanvas = global::System.Windows.Controls.Canvas;
using WpfWindow = global::System.Windows.Window;

namespace Uno.UI.XamlHost.Skia.Wpf;

/// <summary>
/// UnoXamlHost control hosts UWP XAML content inside the Windows Presentation Foundation
/// </summary>
partial class UnoXamlHostBase : IWpfXamlRootHost
{
	private bool _designMode;
	private bool _ignorePixelScaling;
	private WpfCanvas _nativeOverlayLayer;
	private IWpfRenderer _renderer;
	private Windows.UI.Xaml.UIElement? _rootElement;
	private ContentSite _contentSite;
	private WpfWindow? _window;

	/// <summary>
	/// Gets or sets the current Skia Render surface type.
	/// </summary>
	/// <remarks>If <c>null</c>, the host will try to determine the most compatible mode.</remarks>
	public RenderSurfaceType? RenderSurfaceType { get; set; }

	public bool IgnorePixelScaling
	{
		get => _ignorePixelScaling;
		set
		{
			_ignorePixelScaling = value;
			InvalidateVisual();
		}
	}

	private void InitializeHost()
	{
		_contentSite ??= new ContentSite();

		this.IsVisibleChanged += (s, e) =>
		{
			UpdateContentSiteVisible();
		};
		this.Loaded += (s, e) =>
		{
			_window = WpfWindow.GetWindow(this);

			if (_window is not null)
			{
				_window.DpiChanged += OnWindowDpiChanged;
			}
		};

		this.Unloaded += (s, e) =>
		{
			if (_window is null)
			{
				return;
			}

			_window.DpiChanged -= OnWindowDpiChanged;
			_window = null;
		};

		WpfExtensionsRegistrar.Register();

		_designMode = DesignerProperties.GetIsInDesignMode(this);
	}

	private void UpdateContentSiteVisible()
	{
		_contentSite.IsSiteVisible = IsLoaded && IsVisible;
		RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.SiteVisibleChange);
	}

	private void OnWindowDpiChanged(object sender, DpiChangedEventArgs e) => UpdateContentSiteScale();

	private void UpdateContentSiteScale()
	{
		_contentSite.ParentScale = (float)VisualTreeHelper.GetDpi(this).DpiScaleX;
		RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.RasterizationScaleChange);
	}

	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);

		if (!IsXamlContentLoaded())
		{
			return;
		}

		if (_renderer is null)
		{
			_renderer = WpfRendererProvider.CreateForHost(this);
		}

		_renderer?.Render(drawingContext);
	}

	void IXamlRootHost.InvalidateRender()
	{
		ChildInternal?.XamlRoot?.InvalidateOverlays();
		InvalidateVisual();
	}

	WinUI.UIElement? IXamlRootHost.RootElement => _rootElement ??= _xamlSource?.GetVisualTreeRoot();

	WpfCanvas? IWpfXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	bool IWpfXamlRootHost.IgnorePixelScaling => IgnorePixelScaling;

	RenderSurfaceType? IWpfXamlRootHost.RenderSurfaceType => RenderSurfaceType;

	private void RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs args)
	{
		_contentIsland?.RaiseStateChanged(args);
	}
}
