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
	private readonly DisplayInformation _displayInformation;

	private Widget? _area;
	private IGtkRenderer? _renderer;
	private bool _firstSizeAllocated;

	public UnoGtkWindowHost(Window gtkWindow, WinUIWindow winUIWindow)
	{
		_gtkWindow = gtkWindow;
		_winUIWindow = winUIWindow;
		_displayInformation = DisplayInformation.GetForCurrentView();

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

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

		var overlay = new Overlay();

		_area = (Widget)_renderer;

		_displayInformation.DpiChanged += OnDpiChanged;

		_area.Realized += (s, e) =>
		{
			SizeChanged?.Invoke(this, new Windows.Foundation.Size(_area.AllocatedWidth, _area.AllocatedHeight));
		};

		_area.SizeAllocated += (s, e) =>
		{
			SizeChanged?.Invoke(this, new Windows.Foundation.Size(e.Allocation.Width, e.Allocation.Height));
			if (!_firstSizeAllocated)
			{
				_firstSizeAllocated = true;
				// TODO:MZ: Better place for this?
				//_winUIWindow.OnNativeWindowCreated();
			}
		};

		overlay.Add(_area);
		overlay.AddOverlay(_nativeOverlayLayer);
		_eventBox.Add(overlay);
		_gtkWindow.Add(_eventBox);
	}

	internal event EventHandler<Size>? SizeChanged;

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
		GtkManager.XamlRootMap.Register(xamlRoot, this);

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	}

	void IXamlRootHost.InvalidateRender()
	{
		_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();

		_renderer?.InvalidateRender();
	}
}
