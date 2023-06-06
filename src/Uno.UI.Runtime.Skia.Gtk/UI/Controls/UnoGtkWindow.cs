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
using IOPath = System.IO.Path;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;
#pragma warning disable CS0649
#pragma warning disable CS0169

internal class UnoGtkWindow : Gtk.Window, IGtkXamlRootHost
{
	private readonly WinUIWindow _window;
	private FocusManager? _focusManager;

	private static UnoEventBox? _eventBox;
	private Widget? _area;
	private Fixed? _fix;

	private IGtkRenderer? _renderer;

	private GtkDisplayInformationExtension? _displayInformationExtension;
	private CompositeDisposable _disposables = new();

	private record PendingWindowStateChangedInfo(Gdk.WindowState newState, Gdk.WindowState changedMask);
	private List<PendingWindowStateChangedInfo>? _pendingWindowStateChanged = new();

	public UnoGtkWindow(WinUIWindow window) : base(WindowType.Toplevel)
	{
		_window = window;
		_window.Shown += OnShown;
		Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Size.Empty)
		{
			SetDefaultSize((int)preferredWindowSize.Width, (int)preferredWindowSize.Height);
		}
		else
		{
			SetDefaultSize(1024, 800);
		}
		SetPosition(Gtk.WindowPosition.Center);
		Realized += (s, e) =>
		{
			// Load the correct cursors before the window is shown
			// but after the window has been initialized.
			Cursors.EnsureLoaded();
		};

		DeleteEvent += WindowClosing;

		WindowStateEvent += OnWindowStateChanged;

		//SetupRenderSurface();

		var overlay = new Overlay();

		_eventBox = new UnoEventBox();

		_renderSurface = BuildRenderSurfaceType();
		_area = (Widget)_renderSurface;
		_fix = new Fixed();
		overlay.Add(_area);
		overlay.AddOverlay(_fix);
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

	WinUI.UIElement? IXamlRootHost.RootElement => _window.RootElement;

	WinUI.XamlRoot? IXamlRootHost.XamlRoot => _window.RootElement?.XamlRoot;

	RenderSurfaceType? IGtkXamlRootHost.RenderSurfaceType => GtkHost.Current!.RenderSurfaceType;

	private void OnShown(object? sender, EventArgs e) => ShowAll();

	Fixed? IGtkXamlRootHost.NativeOverlayLayer => GtkCoreWindowExtension.FindNativeOverlayLayer(this);

	internal static UnoEventBox? EventBox => _eventBox;

	private void OnWindowStateChanged(object o, WindowStateEventArgs args)
	{
		//var newState = args.Event.NewWindowState;
		//var changedMask = args.Event.ChangedMask;

		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().Debug($"OnWindowStateChanged: {newState}/{changedMask}");
		//}

		//if (_area != null)
		//{
		//	ProcessWindowStateChanged(newState, changedMask);
		//}
		//else
		//{
		//	// Store state changes to replay once the application has been
		//	// initalized completely (initialization can be delayed if the render
		//	// surface is automatically detected).
		//	_pendingWindowStateChanged?.Add(new(newState, changedMask));
		//}
	}

	private void ReplayPendingWindowStateChanges()
	{
		//if (_pendingWindowStateChanged is not null)
		//{
		//	foreach (var state in _pendingWindowStateChanged)
		//	{
		//		ProcessWindowStateChanged(state.newState, state.changedMask);
		//	}

		//	_pendingWindowStateChanged = null;
		//}
	}

	private static void ProcessWindowStateChanged(Gdk.WindowState newState, Gdk.WindowState changedMask)
	{
		//var winUIApplication = WUX.Application.Current;
		//var winUIWindow = WUX.Window.Current;

		//var isVisible =
		//	!(newState.HasFlag(Gdk.WindowState.Withdrawn) ||
		//	newState.HasFlag(Gdk.WindowState.Iconified));

		//var isVisibleChanged =
		//	changedMask.HasFlag(Gdk.WindowState.Withdrawn) ||
		//	changedMask.HasFlag(Gdk.WindowState.Iconified);

		//var focused = newState.HasFlag(Gdk.WindowState.Focused);
		//var focusChanged = changedMask.HasFlag(Gdk.WindowState.Focused);

		//if (!focused && focusChanged)
		//{
		//	winUIWindow?.RaiseActivated(Windows.UI.Core.CoreWindowActivationState.Deactivated);
		//}

		//if (isVisibleChanged)
		//{
		//	if (isVisible)
		//	{
		//		winUIApplication?.RaiseLeavingBackground(() => winUIWindow?.OnVisibilityChanged(true));
		//	}
		//	else
		//	{
		//		winUIWindow?.OnVisibilityChanged(false);
		//		winUIApplication?.RaiseEnteredBackground(null);
		//	}
		//}

		//if (focused && focusChanged)
		//{
		//	winUIWindow?.RaiseActivated(Windows.UI.Core.CoreWindowActivationState.CodeActivated);
		//}
	}

	private void UpdateWindowPropertiesFromPackage()
	{
		if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
		{
			var basePath = uri.OriginalString.Replace('\\', IOPath.DirectorySeparatorChar);
			var iconPath = IOPath.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				SetIconFromFile(iconPath);
			}
			else if (Windows.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				SetIconFromFile(scaledPath);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
			}
		}

		Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
	}

	private void RegisterForBackgroundColor()
	{
		UpdateRendererBackground();

		_disposables.Add(_window.RegisterBackgroundChangedEvent((s, e) => UpdateRendererBackground()));
	}

	private void UpdateRendererBackground()
	{
		if (_window.Background is WinUI.Media.SolidColorBrush brush)
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
		_window.RootElement?.XamlRoot?.InvalidateOverlays();

		_renderer?.InvalidateRender();
	}

	private void WindowClosing(object sender, DeleteEventArgs args)
	{
		//var manager = SystemNavigationManagerPreview.GetForCurrentView();
		//if (!manager.HasConfirmedClose)
		//{
		//	if (!manager.RequestAppClose())
		//	{
		//		// App closing was prevented, handle event
		//		args.RetVal = true;
		//		return;
		//	}
		//}

		//// Closing should continue, perform suspension.
		//WinUIApplication.Current.RaiseSuspending();

		//// All prerequisites passed, can safely close.
		//args.RetVal = false;
		//Gtk.Main.Quit();
	}


	public void TakeScreenshot(string filePath)
	{
		//if (_area is IRenderSurface renderSurface)
		//{
		//	renderSurface.TakeScreenshot(filePath);
		//}
	}
}
