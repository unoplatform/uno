#nullable enable

using System;
using System.Threading.Tasks;
using Gtk;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.GTK.Hosting;
using Uno.UI.Runtime.Skia.GTK.Rendering;
using Uno.UI.Xaml.Core;
using Uno.UI.XamlHost.Skia.GTK.Hosting;
using WinUI = Windows.UI.Xaml;
using WinUIWindow = Windows.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.GTK.UI.Controls;

internal class UnoGtkWindowHost : IGtkXamlRootHost
{
	private readonly Window _gtkWindow;
	private readonly WinUIWindow _winUIWindow;
	private readonly UnoEventBox _eventBox = new();
	private readonly Fixed _nativeOverlayLayer = new();
	private readonly CompositeDisposable _disposables = new();

	private Widget? _area;
	private IGtkRenderer? _renderer;

	public UnoGtkWindowHost(Gtk.Window gtkWindow, WinUIWindow winUIWindow)
	{
		_gtkWindow = gtkWindow;
		_winUIWindow = winUIWindow;

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

		RegisterForBackgroundColor();
	}

	UnoEventBox IGtkXamlRootHost.EventBox => _eventBox;

	Container IGtkXamlRootHost.RootContainer => _gtkWindow;

	bool IXamlRootHost.IsIsland => false;

	WinUI.UIElement? IXamlRootHost.RootElement => _winUIWindow.RootElement;

	WinUI.XamlRoot? IXamlRootHost.XamlRoot => _winUIWindow.RootElement?.XamlRoot;

	RenderSurfaceType? IGtkXamlRootHost.RenderSurfaceType => GtkHost.Current!.RenderSurfaceType;

	Fixed? IGtkXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	async Task IGtkXamlRootHost.InitializeAsync()
	{
		_renderer = await GtkRendererProvider.CreateForHostAsync(this);

		var overlay = new Overlay();

		_area = (Widget)_renderer;
		overlay.Add(_area);
		overlay.AddOverlay(_nativeOverlayLayer);
		_eventBox.Add(overlay);
		_gtkWindow.Add(_eventBox);
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
		XamlRootMap.Register(xamlRoot, this);

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	}

	void IXamlRootHost.InvalidateRender()
	{
		_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();

		_renderer?.InvalidateRender();
	}
}
