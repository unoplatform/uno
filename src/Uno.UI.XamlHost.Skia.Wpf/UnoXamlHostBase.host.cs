#nullable enable

using System.ComponentModel;
using System.Windows.Media;
using WinUI = Microsoft.UI.Xaml;
using WpfCanvas = global::System.Windows.Controls.Canvas;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
using Uno.UI.XamlHost.Extensions;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Runtime.Skia.Wpf.Extensions;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Wpf;

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
	private Microsoft.UI.Xaml.UIElement? _rootElement;

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
		WpfExtensionsRegistrar.Register();

		_designMode = DesignerProperties.GetIsInDesignMode(this);
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
}
