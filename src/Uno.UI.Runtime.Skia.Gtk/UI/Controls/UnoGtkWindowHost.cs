#nullable enable

using System;
using System.Threading.Tasks;
using Gtk;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Rendering;
using Windows.Graphics.Display;
using Windows.Foundation;
using SkiaSharp;
using WinUI = Windows.UI.Xaml;
using WinUIWindow = Windows.UI.Xaml.Window;
using GtkWindow = Gtk.Window;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class UnoGtkWindowHost : IGtkXamlRootHost
{
	private readonly GtkWindow _gtkWindow;
	private readonly WinUIWindow _winUIWindow;
	private readonly UnoEventBox _eventBox = new();
	private readonly Fixed _nativeOverlayLayer = new();
	private readonly CompositeDisposable _disposables = new();

	private XamlRoot? _xamlRoot;
	private IGtkRenderer? _renderer;

	public UnoGtkWindowHost(GtkWindow gtkWindow, WinUIWindow winUIWindow)
	{
		_gtkWindow = gtkWindow;
		_winUIWindow = winUIWindow;

		RegisterForBackgroundColor();
	}

	public GtkWindow GtkWindow => _gtkWindow;

	public UnoEventBox EventBox => _eventBox;

	public Container RootContainer => _gtkWindow;

	public WinUI.UIElement? RootElement => _winUIWindow.RootElement;

	public RenderSurfaceType? RenderSurfaceType => GtkHost.Current!.RenderSurfaceType;

	public Fixed? NativeOverlayLayer => _nativeOverlayLayer;

	public async Task InitializeAsync()
	{
		_renderer = await GtkRendererProvider.CreateForHostAsync(this);
		UpdateRendererBackground();
		_renderer.BackgroundColor = SKColors.Transparent;

		var area = _renderer is GLRenderSurfaceBase ? (Widget)_renderer : new Box(Orientation.Vertical, 0);

		_xamlRoot = GtkManager.XamlRootMap.GetRootForHost(this);
		_xamlRoot!.Changed += OnXamlRootChanged;

		// Subscribing to _area or _gtkWindow should yield similar results, except on WSL,
		// where _gtkWindow.AllocatedHeight is a lot bigger than it actually is for some reason.
		// Either way, make sure to match the subscription with the size, i.e. either use
		// _area.Realized/SizeAllocated and _area.AllocatedXX or _gtkWindow.Realized/SizeAllocation
		// and _gtkWindow.AllocatedXX
		area.Realized += (s, e) =>
		{
			UpdateWindowSize(area.AllocatedWidth, area.AllocatedHeight);
		};

		var overlay = new Overlay();
		overlay.Add(area);
		overlay.AddOverlay(_nativeOverlayLayer);

		// we don't enable airspace when using OpenGL due to problems with transparency
		if (_renderer is SoftwareRenderSurface)
		{
			var area2 = (Widget)_renderer;
			// PassThrough makes it so that any pointer event will fall through.
			// We can't selectively pass certain events through, so we can either
			// pass through all the events, or none of them. We go with the
			// former. This means that clicking on a popup on top of a native element
			// will pass the pointer event to the native element even if it's supposed
			// to be hidden behind the popup.
			area2.Realized += (s, e) =>
			{
				area2.Window.PassThrough = true;
			};

			area.SizeAllocated += (s, e) =>
			{
				UpdateWindowSize(e.Allocation.Width, e.Allocation.Height);
			};
			overlay.AddOverlay(area2);
			overlay.SetOverlayPassThrough(area2, true);
		}

		_eventBox.Add(overlay);
		_gtkWindow.Add(_eventBox);
	}

	internal event EventHandler<Size>? SizeChanged;

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) =>
		UpdateWindowSize(_gtkWindow.AllocatedWidth, _gtkWindow.AllocatedHeight);

	private void UpdateWindowSize(int nativeWidth, int nativeHeight)
	{
		var sizeAdjustment = _xamlRoot?.FractionalScaleAdjustment ?? 1.0;
		SizeChanged?.Invoke(this, new Windows.Foundation.Size(nativeWidth / sizeAdjustment, nativeHeight / sizeAdjustment));
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

	void IXamlRootHost.InvalidateRender()
	{
		_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();

		_renderer?.InvalidateRender();
	}

	public void TakeScreenshot(string filePath) => _renderer?.TakeScreenshot(filePath);
}
