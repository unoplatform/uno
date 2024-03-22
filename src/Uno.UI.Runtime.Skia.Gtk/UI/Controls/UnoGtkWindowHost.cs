#nullable enable

using System;
using System.Threading.Tasks;
using Gtk;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Hosting;
using Uno.UI.Runtime.Skia.Gtk.Rendering;
using Uno.UI.Xaml.Core;
using Windows.Graphics.Display;
using Windows.Foundation;
using WinUI = Microsoft.UI.Xaml;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class UnoGtkWindowHost : IGtkXamlRootHost
{
	private readonly Window _gtkWindow;
	private readonly WinUIWindow _winUIWindow;
	private readonly UnoEventBox _eventBox = new();
	private readonly Fixed _nativeOverlayLayer = new();
	private readonly CompositeDisposable _disposables = new();

	private DisplayInformation? _displayInformation;
	private Widget? _area;
	private IGtkRenderer? _renderer;
	private bool _firstSizeAllocated;

	public UnoGtkWindowHost(Window gtkWindow, WinUIWindow winUIWindow)
	{
		_gtkWindow = gtkWindow;
		_winUIWindow = winUIWindow;

		RegisterForBackgroundColor();
	}

	public Window GtkWindow => _gtkWindow;

	public UnoEventBox EventBox => _eventBox;

	public Container RootContainer => _gtkWindow;

	public WinUI.UIElement? RootElement => _winUIWindow.RootElement;

	public RenderSurfaceType? RenderSurfaceType => GtkHost.Current!.RenderSurfaceType;

	public Fixed? NativeOverlayLayer => _nativeOverlayLayer;

	public IGtkRenderer? Renderer => _renderer;

	public async Task InitializeAsync()
	{
		_renderer = await GtkRendererProvider.CreateForHostAsync(this);
		UpdateRendererBackground();

		var overlay = new Overlay();

		_area = (Widget)_renderer;

		var xamlRoot = GtkManager.XamlRootMap.GetRootForHost(this);
		_displayInformation = WinUI.XamlRoot.GetDisplayInformation(xamlRoot);
		_displayInformation.DpiChanged += OnDpiChanged;

		UpdateWindowSize(_gtkWindow.Allocation.Width, _gtkWindow.Allocation.Height);

		// Subcribing to _area or _gtkWindow should yield similar results, except that
		// we explicitly set the DefaultSize on the window not the area, so the area
		// will start out with size 1x1 and then after layouting is finished, will end up
		// with the correct size. To avoid triggering multiple window size updates, we
		// specifically choose to subscribe to the _gtkWindow not the _area
		_gtkWindow.Realized += (s, e) =>
		{
			UpdateWindowSize(_gtkWindow.AllocatedWidth, _gtkWindow.AllocatedHeight);
		};

		_gtkWindow.SizeAllocated += (s, e) =>
		{
			UpdateWindowSize(e.Allocation.Width, e.Allocation.Height);
			if (!_firstSizeAllocated)
			{
				_firstSizeAllocated = true;
			}
		};

		overlay.Add(_area);
		overlay.AddOverlay(_nativeOverlayLayer);
		_eventBox.Add(overlay);
		_gtkWindow.Add(_eventBox);
	}

	internal event EventHandler<Size>? SizeChanged;

	private void OnDpiChanged(DisplayInformation sender, object args) =>
		UpdateWindowSize(_gtkWindow.AllocatedWidth, _gtkWindow.AllocatedHeight);

	private void UpdateWindowSize(int nativeWidth, int nativeHeight)
	{
		var sizeAdjustment = _displayInformation!.FractionalScaleAdjustment;
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
}
