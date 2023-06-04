#nullable enable

using System;
using System.Collections.Generic;
using Gtk;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.GTK.UI.Core;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.WebUI;
using Windows.UI.Xaml;
using WinUIWindow = Windows.UI.Xaml.Window;
using UnoApplication = Windows.UI.Xaml.Application;
using WinUI = Windows.UI.Xaml;
using Uno.UI.Runtime.Skia.GTK.Hosting;
using Uno.UI.Xaml.Hosting;

namespace Uno.UI.Runtime.Skia.UI.Xaml.Controls;
#pragma warning disable CS0649
#pragma warning disable CS0169

internal class UnoGtkWindow : Gtk.Window, IGtkWindowHost
{
	private readonly WinUIWindow _window;

	private static UnoEventBox? _eventBox;
	private Widget? _area;
	private Fixed? _fix;

	private IRenderSurface? _renderSurface;

	private GtkDisplayInformationExtension? _displayInformationExtension;
	private CompositeDisposable _registrations = new();

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

		///* avoids double invokes at window level */
		//_area.AddEvents((int)GtkCoreWindowExtension.RequestedEvents);


		CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet += OnCoreWindowContentRootSet;

		RegisterForBackgroundColor();
		UpdateWindowPropertiesFromPackage();

		//ReplayPendingWindowStateChanges();
	}

	private void OnShown(object? sender, EventArgs e) => ShowAll();

	internal IRenderSurface? RenderSurface => _renderSurface;

	internal Fixed? NativeOverlayLayer => GtkCoreWindowExtension.FindNativeOverlayLayer(this);

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
		//if (Windows.ApplicationModel.Package.Current.Logo is Uri uri)
		//{
		//	var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
		//	var iconPath = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledPath, basePath);

		//	if (File.Exists(iconPath))
		//	{
		//		if (this.Log().IsEnabled(LogLevel.Information))
		//		{
		//			this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
		//		}

		//		GtkHost.Window.SetIconFromFile(iconPath);
		//	}
		//	else if (Windows.UI.Xaml.Media.Imaging.BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
		//	{
		//		if (this.Log().IsEnabled(LogLevel.Information))
		//		{
		//			this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
		//		}

		//		GtkHost.Window.SetIconFromFile(scaledPath);
		//	}
		//	else
		//	{
		//		if (this.Log().IsEnabled(LogLevel.Warning))
		//		{
		//			this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
		//		}
		//	}
		//}

		//Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().Title = Windows.ApplicationModel.Package.Current.DisplayName;
	}


	private IRenderSurface BuildRenderSurfaceType()
		=> GtkHost.Current!.RenderSurfaceType switch
		{
			Skia.RenderSurfaceType.OpenGLES => new OpenGLESRenderSurface(this),
			Skia.RenderSurfaceType.OpenGL => new OpenGLRenderSurface(this),
			Skia.RenderSurfaceType.Software => new SoftwareRenderSurface(this),
			_ => throw new InvalidOperationException($"Unsupported RenderSurfaceType {GtkHost.Current!.RenderSurfaceType}")
		};


	//private void SetupRenderSurface()
	//{
	//	TryReadRenderSurfaceTypeEnvironment();

	//	if (!OpenGLRenderSurface.IsSupported && !OpenGLESRenderSurface.IsSupported)
	//	{
	//		// Pre-validation is required to avoid initializing OpenGL on macOS
	//		// where the whole app may get visually corrupted even if OpenGL is not
	//		// used in the app.

	//		if (this.Log().IsEnabled(LogLevel.Debug))
	//		{
	//			this.Log().Debug($"Neither OpenGL or OpenGL ES are supporting, using software rendering");
	//		}

	//		GtkHost.Current!.RenderSurfaceType = Skia.RenderSurfaceType.Software;
	//	}

	//	if (GtkHost.Current!.RenderSurfaceType == null)
	//	{
	//		// Create a temporary surface to automatically detect
	//		// the OpenGL environment that can be used on the system.
	//		GLValidationSurface validationSurface = new();

	//		Add(validationSurface);
	//		ShowAll();

	//		DispatchNativeSingle(ValidatedSurface);

	//		async void ValidatedSurface()
	//		{
	//			try
	//			{
	//				if (this.Log().IsEnabled(LogLevel.Debug))
	//				{
	//					this.Log().Debug($"Auto-detecting surface type");
	//				}

	//				// Wait for a realization of the GLValidationSurface
	//				RenderSurfaceType = await validationSurface.GetSurfaceTypeAsync();

	//				// Continue on the GTK main thread
	//				DispatchNativeSingle(() =>
	//				{
	//					if (this.Log().IsEnabled(LogLevel.Debug))
	//					{
	//						this.Log().Debug($"Auto-detected {RenderSurfaceType} rendering");
	//					}

	//					_window.Remove(validationSurface);

	//					FinalizeStartup();
	//				});
	//			}
	//			catch (Exception e)
	//			{
	//				if (this.Log().IsEnabled(LogLevel.Error))
	//				{
	//					this.Log().Error($"Auto-detected failed", e);
	//				}
	//			}
	//		}
	//	}
	//	else
	//	{
	//		FinalizeStartup();
	//	}
	//}

	private void TryReadRenderSurfaceTypeEnvironment()
	{
		//if (Enum.TryParse(Environment.GetEnvironmentVariable("UNO_RENDER_SURFACE_TYPE"), out RenderSurfaceType surfaceType))
		//{
		//	if (this.Log().IsEnabled(LogLevel.Debug))
		//	{
		//		this.Log().Debug($"Overriding RnderSurfaceType using command line with {surfaceType}");
		//	}

		//	RenderSurfaceType = surfaceType;
		//}
	}

	private void RegisterForBackgroundColor()
	{
		//if (_area is IRenderSurface renderSurface)
		//{
		//	void Update()
		//	{
		//		if (WUX.Window.Current.Background is WUX.Media.SolidColorBrush brush)
		//		{
		//			renderSurface.BackgroundColor = brush.Color;
		//		}
		//		else
		//		{
		//			if (this.Log().IsEnabled(LogLevel.Warning))
		//			{
		//				this.Log().Warn($"This platform only supports SolidColorBrush for the Window background");
		//			}
		//		}

		//	}

		//	Update();

		//	_registrations.Add(WUX.Window.Current.RegisterBackgroundChangedEvent((s, e) => Update()));
		//}
	}

	private void OnCoreWindowContentRootSet(object sender, object e)
	{
		//var xamlRoot = CoreServices.Instance
		//	.ContentRootCoordinator
		//	.CoreWindowContentRoot?
		//	.GetOrCreateXamlRoot();

		//if (xamlRoot is null)
		//{
		//	throw new InvalidOperationException("XamlRoot was not properly initialized");
		//}

		//XamlRootMap.Register(xamlRoot, this);
		//xamlRoot.InvalidateRender += _renderSurface.InvalidateRender;

		//CoreServices.Instance.ContentRootCoordinator.CoreWindowContentRootSet -= OnCoreWindowContentRootSet;
	}

	void IXamlRootHost.InvalidateRender()
	{
		//InvalidateOverlays();
		_renderSurface?.InvalidateRender();
	}

	bool IXamlRootHost.IsIsland => false;

	WinUI.UIElement? IXamlRootHost.RootElement => _window.RootElement;

	WinUI.XamlRoot? IXamlRootHost.XamlRoot => _window.RootElement?.XamlRoot;

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
