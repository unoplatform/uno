#nullable enable

using System;
using System.Collections.Generic;
using Gtk;
using Uno.Disposables;
using Uno.UI.Runtime.Skia.GTK.UI.Core;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using WinUIWindow = Windows.UI.Xaml.Window;
using WinUI = Windows.UI.Xaml;
using Uno.UI.Runtime.Skia.GTK.Hosting;
using Uno.UI.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Uno.UI.Rendering;
using Uno.UI.XamlHost.Skia.GTK.Hosting;
using System.IO;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.GTK.UI.Controls;

internal class UnoGtkWindowHost : IGtkXamlRootHost
{
	private readonly Window _gtkWindow;
	private readonly WinUIWindow _winUIWindow;
	private FocusManager? _focusManager;

	private static UnoEventBox? _eventBox;
	private Widget? _area;
	private Fixed? _nativeOverlayLayer;

	private IGtkRenderer? _renderer;

	private GtkDisplayInformationExtension? _displayInformationExtension;
	private CompositeDisposable _disposables = new();


	public UnoGtkWindowHost(Gtk.Window gtkWindow, WinUIWindow winUIWindow)
	{
		_gtkWindow = gtkWindow;
		_winUIWindow = winUIWindow;

		//SetupRenderSurface();

		var overlay = new Overlay();

		_eventBox = new UnoEventBox();

		_renderSurface = BuildRenderSurfaceType();
		_area = (Widget)_renderSurface;
		_nativeOverlayLayer = new Fixed();
		overlay.Add(_area);
		overlay.AddOverlay(_nativeOverlayLayer);
		_eventBox.Add(overlay);
		Add(_eventBox);

		//// Show the whole tree again, since we may have
		//// swapped the content with the GLValidationSurface.
		//_window.ShowAll();

		//if (this.Log().IsEnabled(LogLevel.Information))
		//{
		//	this.Log().Info($"Using {RenderSurfaceType} rendering");
		//}

		//_area.Realized += (s, e) =>
		//{
		//	WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(_area.AllocatedWidth, _area.AllocatedHeight));
		//};

		//_area.SizeAllocated += (s, e) =>
		//{
		//	WUX.Window.Current.OnNativeSizeChanged(new Windows.Foundation.Size(e.Allocation.Width, e.Allocation.Height));
		//};

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

		RegisterForBackgroundColor();
		UpdateWindowPropertiesFromPackage();

		//ReplayPendingWindowStateChanges();
	}

	bool IXamlRootHost.IsIsland => false;

	WinUI.UIElement? IXamlRootHost.RootElement => _winUIWindow.RootElement;

	WinUI.XamlRoot? IXamlRootHost.XamlRoot => _winUIWindow.RootElement?.XamlRoot;

	RenderSurfaceType? IGtkXamlRootHost.RenderSurfaceType => GtkHost.Current!.RenderSurfaceType;

	Fixed? IGtkXamlRootHost.NativeOverlayLayer => _nativeOverlayLayer;

	internal static UnoEventBox? EventBox => _eventBox;

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
		xamlRoot.InvalidateRender += _renderer!.InvalidateRender;

		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	}

	void IXamlRootHost.InvalidateRender()
	{
		_winUIWindow.RootElement?.XamlRoot?.InvalidateOverlays();

		_renderer?.InvalidateRender();
	}
}
