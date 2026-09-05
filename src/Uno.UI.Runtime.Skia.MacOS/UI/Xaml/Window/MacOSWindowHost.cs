using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Input;
using PointerEventArgs = global::Windows.UI.Core.PointerEventArgs;
using PointerDeviceType = global::Windows.Devices.Input.PointerDeviceType;
using KeyEventArgs = global::Windows.UI.Core.KeyEventArgs;
using CharacterReceivedEventArgs = global::Windows.UI.Core.CharacterReceivedEventArgs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.Hosting;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media;
using Window = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowHost : IXamlRootHost, IUnoKeyboardInputSource, IUnoCorePointerInputSource, IAccessibilityOwner
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly MacOSWindowNative _nativeWindow;
	private readonly Window _winUIWindow;
	private readonly XamlRoot _xamlRoot;
	private readonly DisplayInformation _displayInformation;
	private readonly GRContext? _context;
	private MacOSRenderThread? _metalRenderThread;
	private SKBitmap? _bitmap;
	private SKSurface? _surface;
	private readonly RetainedLayer _retainedLayer = new();
	private int _rowBytes;
	private volatile bool _initializationCompleted;
	// Written by the software/legacy draw paths on the main thread and by the Metal render thread.
	private volatile string? _lastSvgClipPath;
	private Size _nativeWindowSize;
	private MacOSAccessibility? _accessibility;
	private bool _accessibilityBuildQueued;

	public MacOSWindowHost(MacOSWindowNative nativeWindow, Window winUIWindow, XamlRoot xamlRoot)
	{
		_nativeWindow = nativeWindow ?? throw new ArgumentNullException(nameof(nativeWindow));
		_winUIWindow = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));
		_xamlRoot = xamlRoot ?? throw new ArgumentNullException(nameof(xamlRoot));
		_displayInformation = DisplayInformation.GetOrCreateForWindowId(winUIWindow.AppWindow.Id);

		// RegisterForBackgroundColor();

		var host = MacSkiaHost.Current;
		switch (host.RenderSurfaceType)
		{
			case RenderSurfaceType.Metal:
				NativeUno.uno_window_get_metal_handles(_nativeWindow.Handle, out var device, out var queue);
				var ctx = new GRMtlBackendContext()
				{
					DeviceHandle = device,
					QueueHandle = queue,
				};
				_context = GRContext.CreateMetal(ctx);
				InitializeMetalRenderThread();
				break;
			case RenderSurfaceType.Software:
				break;
		}
	}

	// Display

	internal event EventHandler<PointInt32>? PositionChanged;

	internal event EventHandler<Size>? SizeChanged;

	internal event EventHandler? RasterizationScaleChanged;

	internal double RasterizationScale => _displayInformation.RawPixelsPerViewPixel;

	private void UpdateWindowSize(double nativeWidth, double nativeHeight)
	{
		_nativeWindowSize = new Size(nativeWidth, nativeHeight);
		SizeChanged?.Invoke(this, _nativeWindowSize);
	}

	private void InitializeMetalRenderThread()
	{
		if (_context is null)
		{
			return;
		}

		var screenFps = NativeUno.uno_window_get_refresh_rate(_nativeWindow.Handle);
		var targetFps = ResolveTargetFps(screenFps);

		// Information rather than Trace on purpose: on a CI agent this is the only record of what the
		// render clock is actually running at, and a screen reporting an unexpected rate (or none)
		// is the first thing to check when frames stall.
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info(
				$"macOS render thread starting for window {_nativeWindow.Handle}: " +
				$"surface={MacSkiaHost.Current.RenderSurfaceType}, " +
				$"screen refresh rate={(screenFps > 0 ? screenFps.ToString("0.##", CultureInfo.InvariantCulture) + "Hz" : "unknown")}, " +
				$"pacing at {targetFps.ToString("0.##", CultureInfo.InvariantCulture)} fps.");
		}

		_metalRenderThread = new MacOSRenderThread(_nativeWindow.Handle, _context, RenderThreadMetalDraw, targetFps);
	}

	/// <summary>
	/// Frame rate the render thread should be paced at: the screen's refresh rate when it is known
	/// and <see cref="FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate"/> is set,
	/// otherwise the configured rate.
	/// </summary>
	private static double ResolveTargetFps(double screenFps)
		=> FeatureConfiguration.CompositionTarget.SetFrameRateAsScreenRefreshRate && screenFps > 0
			? screenFps
			: FeatureConfiguration.CompositionTarget.FrameRate;

	/// <summary>
	/// Called on the render thread. Draws the recorded SKPicture into the Metal texture
	/// acquired from the layer; the render loop flushes and presents after this returns.
	/// </summary>
	private void RenderThreadMetalDraw(double nativeWidth, double nativeHeight, nint texture)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_nativeWindow.Handle} render thread drawing {nativeWidth}x{nativeHeight} texture: {texture}");
		}

		if (RootElement?.Visual.CompositionTarget is not CompositionTarget ct)
		{
			return;
		}

		// FIXME: we get the first (native) updates for window sizes before we have completed the (managed)
		// host initialization — https://github.com/unoplatform/uno-private/issues/319
		// The non-threaded path re-delivers the size from every draw until the managed host has subscribed
		// to SizeChanged. Without that here, a size that arrives too early is simply lost and XamlRoot.Bounds
		// stays 0x0 — and CoreServices.RequestAdditionalFrame silently does nothing while bounds are zero
		// (CoreServices.cs:67), so UpdateLayout and RaiseLoadedEvent never run for that window. Every
		// WaitForLoaded against it then burns its full timeout, three times over, and the test fails.
		if (!_initializationCompleted)
		{
			// UpdateWindowSize raises SizeChanged into the managed tree, so it must run on the UI thread.
			NativeDispatcher.Main.Enqueue(() =>
			{
				var scale = (float)_xamlRoot.RasterizationScale;
				UpdateWindowSize(nativeWidth / scale, nativeHeight / scale);
				_initializationCompleted = SizeChanged is not null;

				// Nothing else will ask for the next frame while we are still bootstrapping, and the
				// request is paced, so this cannot spin.
				_metalRenderThread?.RequestFrame();
			}, NativeDispatcherPriority.Normal);

			return;
		}

		// The app is drawn into a retained layer sized from the managed XamlRoot bounds, which can
		// briefly disagree with the drawable size while a resize is in flight. Only the blit below
		// targets the drawable, so the swapchain surface must use the texture's own dimensions.
		// The layer also survives across frames, which is what keeps damage-region rendering usable
		// even though the drawable rotates every frame.
		var nativeElementClipPath = ct.OnNativePlatformFrameRequested(
			_retainedLayer.Surface?.Canvas,
			size => _retainedLayer.EnsureSurface(_context!, (int)size.Width, (int)size.Height, SKColors.Transparent).Canvas);

		using (var target = new GRBackendRenderTarget((int)nativeWidth, (int)nativeHeight, new GRMtlTextureInfo(texture)))
		using (var swapchainSurface = SKSurface.Create(_context, target, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888))
		{
			_retainedLayer.Present(swapchainSurface);
		}

		// uno_window_clip_svg mutates AppKit view layers, which must be touched only on the
		// main thread; this method runs on the render thread, so marshal the update there.
		var clip = nativeElementClipPath.IsEmpty ? null : nativeElementClipPath.ToSvgPathData();
		if (clip != _lastSvgClipPath)
		{
			// Written before the enqueue so a subsequent render-thread frame producing the same clip
			// doesn't queue a duplicate update while this one is still pending. MetalDraw can write
			// on success instead because it already runs on the main thread.
			_lastSvgClipPath = clip;
			NativeDispatcher.Main.Enqueue(() =>
			{
				// if too early it's possible that the native element has not been arranged yet
				// so the position and dimension of the element are not yet correct (0,0,0,0)
				if (!NativeUno.uno_window_clip_svg(_nativeWindow.Handle, clip) && _lastSvgClipPath == clip)
				{
					// Retry on the next frame, unless a newer clip was recorded meanwhile — that one
					// has its own pending update and must not be forced to re-apply.
					_lastSvgClipPath = null;
				}
			}, NativeDispatcherPriority.Normal);
		}
	}

	private void MetalDraw(double nativeWidth, double nativeHeight, nint texture)
	{
		if (_metalRenderThread is not null)
		{
			// The render thread owns the GRContext and the retained layer on the Metal path. AppKit
			// should not reach this (the view is paused), but bail out rather than risk drawing from
			// the main thread concurrently with it.
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_nativeWindow.Handle} drawing {nativeWidth}x{nativeHeight} texture: {texture}");
		}

		var scale = (float)_xamlRoot.RasterizationScale;

		// FIXME: we get the first (native) updates for window sizes before we have completed the (managed) host initialization
		// https://github.com/unoplatform/uno-private/issues/319
		if (!_initializationCompleted)
		{
			UpdateWindowSize(nativeWidth / scale, nativeHeight / scale);
			_initializationCompleted = SizeChanged is not null;
			if (!_initializationCompleted)
			{
				return; // not yet...
			}
		}

		var nativeElementClipPath = ((CompositionTarget)RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(
			_retainedLayer.Surface?.Canvas,
			size => _retainedLayer.EnsureSurface(_context!, (int)size.Width, (int)size.Height, SKColors.Transparent).Canvas);

		using (var target = new GRBackendRenderTarget((int)nativeWidth, (int)nativeHeight, new GRMtlTextureInfo(texture)))
		using (var swapchainSurface = SKSurface.Create(_context, target, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888))
		{
			_retainedLayer.Present(swapchainSurface);
		}

		var clip = nativeElementClipPath.IsEmpty ? null : nativeElementClipPath.ToSvgPathData();
		if (clip != _lastSvgClipPath)
		{
			// if too early it's possible that the native element has not been arranged yet
			// so the position and dimension of the element are not yet correct (0,0,0,0)
			if (NativeUno.uno_window_clip_svg(_nativeWindow.Handle, clip))
			{
				_lastSvgClipPath = clip;
			}
		}

		_context?.Flush();
	}

	private unsafe void SoftDraw(double nativeWidth, double nativeHeight, nint* data, int* rowBytes, int* size)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_nativeWindow.Handle} drawing {nativeWidth}x{nativeHeight}");
		}

		var scale = (float)_xamlRoot.RasterizationScale;

		// FIXME: we get the first (native) updates for window sizes before we have completed the (managed) host initialization
		// https://github.com/unoplatform/uno-private/issues/319
		if (!_initializationCompleted)
		{
			UpdateWindowSize(nativeWidth, nativeHeight);
			_initializationCompleted = SizeChanged is not null;
			if (!_initializationCompleted)
			{
				return; // not yet...
			}
		}

		var nativeElementClipPath = ((CompositionTarget)RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(_surface?.Canvas, size =>
		{
			_bitmap?.Dispose();
			_surface?.Dispose();

			var info = new SKImageInfo((int)size.Width, (int)size.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
			_bitmap = new SKBitmap(info);
			_surface = SKSurface.Create(info, _bitmap.GetPixels());
			_rowBytes = info.RowBytes;
			return _surface.Canvas;
		});

		var clip = nativeElementClipPath.IsEmpty ? null : nativeElementClipPath.ToSvgPathData();
		if (clip != _lastSvgClipPath)
		{
			// if too early it's possible that the native element has not been arranged yet
			// so the position and dimension of the element are not yet correct (0,0,0,0)
			if (NativeUno.uno_window_clip_svg(_nativeWindow.Handle, clip))
			{
				_lastSvgClipPath = clip;
			}
		}

		if (_bitmap is not null)
		{
			*data = _bitmap.GetPixels(out var bitmapSize);
			*size = (int)bitmapSize;
			*rowBytes = _rowBytes;
		}
	}

	// Window management

	private static readonly Dictionary<nint, WeakReference<MacOSWindowHost>> _windows = [];

	public static unsafe void Register()
	{
		// From managed code this will load `libSkiaSharp` from `netX0/runtimes/osx/native/libSkiaSharp.dylib` so
		// `libUnoNativeMac.dylib` will find it already available and won't try to load it from `@rpath/libSkiaSharp.dylib`
		NativeSkia.gr_direct_context_make_metal(0, 0);

		NativeUno.uno_set_drawing_callbacks(&MetalDraw, &SoftDraw, &Resize);

		NativeUno.uno_set_window_events_callbacks(&OnRawKeyDown, &OnRawKeyUp, &OnMouseEvent, &OnMoveEvent, &Resize);
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoKeyboardInputSource), o => (o as IUnoKeyboardInputSource)!);
		ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource), o => (o as IUnoCorePointerInputSource)!);

		NativeUno.uno_set_window_close_callbacks(&WindowShouldClose, &WindowClose);

		NativeUno.uno_set_ime_callbacks(&OnImeInsertText, &OnImeSetMarkedText, &OnImeUnmarkText, &OnImeGetCaretRect);

		NativeUno.uno_set_window_screen_change_callbacks(&ScreenChanged, &ScreenParametersChanged);
		ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new MacOSDisplayInformationExtension(o));
	}

	public UIElement? RootElement => _winUIWindow.RootElement;

	SkiaAccessibilityBase? IAccessibilityOwner.Accessibility => _accessibility;

	internal nint NativeWindowHandle => _nativeWindow.Handle;

	internal void InitializeAccessibility()
	{
		if (_accessibility is not null || _nativeWindow.Handle == nint.Zero)
		{
			return;
		}

		_accessibility = new MacOSAccessibility(_nativeWindow.Handle);

		if (_winUIWindow.RootElement is { } rootElement)
		{
			QueueTreeBuild(rootElement);
		}
		else
		{
			// Defer the tree build until the window is activated and has content.
			_winUIWindow.Activated += OnWinUIWindowActivatedForAccessibility;
		}
	}

	private void OnWinUIWindowActivatedForAccessibility(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
	{
		// Sticky active-owner tracking (FR-007, research Decision 3): update on
		// Activated (WA_ACTIVE / NSWindowDidBecomeMainNotification analog), never
		// clear on Deactivated.
		if (args.WindowActivationState != Microsoft.UI.Xaml.WindowActivationState.Deactivated &&
			_accessibility is not null)
		{
			AccessibilityRouter.SetActive(this);
		}

		if (_accessibility is not { IsAccessibilityEnabled: true })
		{
			return;
		}

		if (_winUIWindow.RootElement is { } rootElement)
		{
			QueueTreeBuild(rootElement);
		}
	}

	private void QueueTreeBuild(UIElement rootElement)
	{
		if (_accessibilityBuildQueued)
		{
			return;
		}
		_accessibilityBuildQueued = true;

		_ = rootElement.Dispatcher.RunAsync(
			Windows.UI.Core.CoreDispatcherPriority.Low,
			() =>
			{
				_accessibilityBuildQueued = false;
				if (_accessibility is { IsAccessibilityEnabled: true })
				{
					_accessibility.BuildTree(rootElement);
				}
			});
	}

	internal void DisposeAccessibility()
	{
		if (_accessibility is not { } accessibility)
		{
			return;
		}

		_accessibility = null;
		_winUIWindow.Activated -= OnWinUIWindowActivatedForAccessibility;
		accessibility.Dispose();
		AccessibilityRouter.NotifyDisposed(this);
	}

	void IXamlRootHost.InvalidateRender()
	{
		if (_metalRenderThread is not null)
		{
			// Metal path: ask the dedicated render thread for a frame so draw + present run off the
			// UI thread. The request is paced, so repeated invalidations inside one frame interval
			// coalesce instead of each costing a drawable acquisition.
			_metalRenderThread.RequestFrame();
		}
		else
		{
			// Software path: drive AppKit's display loop on the UI thread.
			NativeUno.uno_window_invalidate(_nativeWindow.Handle);
		}
	}

	void IXamlRootHost.ResignNativeFocus() => NativeUno.uno_window_resign_native_first_responder(_nativeWindow.Handle);

	public static void Register(nint handle, XamlRoot xamlRoot, MacOSWindowHost host)
	{
		XamlRootMap.Register(xamlRoot, host);
		_windows.Add(handle, new WeakReference<MacOSWindowHost>(host));
	}

	private static void Unregister(nint handle) => _windows.Remove(handle);

	/// <summary>
	/// Returns the native window handle for the given XamlRoot, or 0 if not found.
	/// Used by IME extension to activate/deactivate native IME routing.
	/// </summary>
	internal static nint GetNativeHandleForXamlRoot(XamlRoot? xamlRoot)
	{
		if (xamlRoot is null)
		{
			return 0;
		}

		foreach (var (handle, weak) in _windows)
		{
			if (weak.TryGetTarget(out var host) && host._xamlRoot == xamlRoot)
			{
				return handle;
			}
		}

		return 0;
	}

	private static MacOSWindowHost? GetWindowHost(nint handle)
	{
		if (_windows.TryGetValue(handle, out var weak))
		{
			weak.TryGetTarget(out var window);
			return window;
		}
		return null;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void MetalDraw(nint handle, double width, double height, nint texture)
	{
		// This runs directly from a native callback, so an escaping managed exception would
		// fail-fast the process. Route it through the recoverable handler like the other hosts.
		try
		{
			var window = GetWindowHost(handle);
			window?.MetalDraw(width, height, texture);
		}
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSWindowHost));
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void SoftDraw(nint handle, double width, double height, nint* data, int* rowBytes, int* size)
	{
		try
		{
			var window = GetWindowHost(handle);
			window?.SoftDraw(width, height, data, rowBytes, size);
		}
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSWindowHost));
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void Resize(nint handle, double width, double height)
	{
		var window = GetWindowHost(handle);
		if (window is not null)
		{
			window.UpdateWindowSize(width, height);
		}
		else if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Warning))
		{
			// _initializationCompleted takes care of some legit cases where this can happen, e.g. the NSView.window might not yet be set when the view is created but not yet assigned
			typeof(MacOSWindowHost).Log().Warn($"MacOSWindowHost.Resize could not map 0x{handle:X} with an NSWindow");
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnMoveEvent(nint handle, double x, double y)
	{
		var window = GetWindowHost(handle);
		if (window is not null)
		{
			window.PositionChanged?.Invoke(window, new PointInt32((int)x, (int)y));
		}
		// the first event occurs before the managed side is ready to handle it
		// this special case is handled inside MacOSWindowWrapper constructor
	}

	// IUnoKeyboardInputSource

	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
	event TypedEventHandler<object, CharacterReceivedEventArgs>? IUnoKeyboardInputSource.CharacterReceived { add { } remove { } }

	private static KeyEventArgs CreateArgs(VirtualKey key, VirtualKeyModifiers mods, uint scanCode, ushort unicode)
	{
		var status = new CorePhysicalKeyStatus
		{
			ScanCode = scanCode,
		};
		return new KeyEventArgs("keyboard", key, mods, status, unicode == 0 ? null : (char)unicode);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int OnRawKeyDown(nint handle, VirtualKey key, VirtualKeyModifiers mods, uint scanCode, ushort unicode)
	{
		try
		{
			if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSWindowHost).Log().Trace($"OnRawKeyDown '${key}', mods: '{mods}', scanCode: {scanCode}, unicode: {unicode}");
			}

			var window = GetWindowHost(handle);

			// if fullscreen then the OS will return to the default, overlapped window and we need to dispose the current presenter
			if ((key == VirtualKey.Escape) && NativeUno.uno_window_is_full_screen(handle))
			{
				window?._winUIWindow?.AppWindow?.SetPresenter(AppWindowPresenterKind.Default);
				// also notify media player(s) that could be running in (the soon to be not so) full screen
				MacOSMediaPlayerPresenterExtension.OnEscapingFullScreen();
			}

			var keyDown = window?.KeyDown;
			if (keyDown is null)
			{
				return 0;
			}
			var args = CreateArgs(key, mods, scanCode, unicode);
			keyDown.Invoke(window!, args);
			return args.Handled ? 1 : 0;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int OnRawKeyUp(nint handle, VirtualKey key, VirtualKeyModifiers mods, uint scanCode, ushort unicode)
	{
		try
		{
			if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSWindowHost).Log().Trace($"OnRawKeyUp '${key}', mods: '{mods}', scanCode: {scanCode}, unicode: {unicode}");
			}

			var window = GetWindowHost(handle);
			var keyUp = window?.KeyUp;
			if (keyUp is null)
			{
				return 0;
			}
			var args = CreateArgs(key, mods, scanCode, unicode);
			keyUp.Invoke(window!, args);
			return args.Handled ? 1 : 0;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	// IME (Input Method Editor) callbacks for NSTextInputClient composition support

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnImeInsertText(nint handle, ushort* textPtr, int length)
	{
		try
		{
			var text = new string((char*)textPtr, 0, length);
			if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSWindowHost).Log().Trace($"OnImeInsertText: '{text}'");
			}
			MacOSImeTextBoxExtension.Instance.OnInsertText(text);
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnImeSetMarkedText(nint handle, ushort* textPtr, int length, int selectedStart, int selectedLength)
	{
		try
		{
			var text = length > 0 ? new string((char*)textPtr, 0, length) : string.Empty;
			if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSWindowHost).Log().Trace($"OnImeSetMarkedText: '{text}' selected: [{selectedStart}..{selectedStart + selectedLength}]");
			}
			MacOSImeTextBoxExtension.Instance.OnSetMarkedText(text, selectedStart, selectedLength);
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnImeUnmarkText(nint handle)
	{
		try
		{
			if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Trace))
			{
				typeof(MacOSWindowHost).Log().Trace("OnImeUnmarkText");
			}
			MacOSImeTextBoxExtension.Instance.OnUnmarkText();
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnImeGetCaretRect(nint handle, double* x, double* y, double* width, double* height)
	{
		try
		{
			var rect = MacOSImeTextBoxExtension.Instance.GetCaretRect();
			*x = rect.X;
			*y = rect.Y;
			*width = rect.Width;
			*height = rect.Height;
		}
		catch (Exception e)
		{
			*x = *y = *width = *height = 0;
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	// IUnoCorePointerInputSource

	// https://developer.apple.com/documentation/appkit/nseventtype
	private const int NSEventTypeLeftMouseDown = 1;
	private const int NSEventTypeRightMouseDown = 2;
	private const int NSEventTypeOtherMouseDown = 25;

	private CoreCursor? _pointerCursor = new(CoreCursorType.Arrow, 0);

	private static Point _previousPosition;
	private static PointerPointProperties? _previousProperties;

	[NotImplemented] public bool HasCapture => false;

	public CoreCursor? PointerCursor
	{
		get => _pointerCursor;
		set
		{
			if (value is null)
			{
				if (_pointerCursor is not null)
				{
					NativeUno.uno_cursor_hide();
					_pointerCursor = null;
				}
			}
			else
			{
				if (_pointerCursor is null)
				{
					NativeUno.uno_cursor_show();
				}
				_pointerCursor = value;
				if (!NativeUno.uno_cursor_set(_pointerCursor.Type))
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().LogWarning($"Cursor type '{_pointerCursor.Type}' is not supported on macOS. Closest approximation or default cursor is used instead.");
					}
				}
			}
		}
	}

	public Point PointerPosition => _previousPosition;

#pragma warning disable CS0067
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
#pragma warning restore CS0067
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
#pragma warning disable CS0067
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static unsafe int OnMouseEvent(nint handle, NativeMouseEventData* data)
	{
		try
		{
			var window = GetWindowHost(handle);
			if (window is null)
			{
				return 0; // unhandled
			}

			TypedEventHandler<object, PointerEventArgs>? mouseEvent = null;
			switch (data->EventType)
			{
				case NativeMouseEvents.Entered:
					mouseEvent = window.PointerEntered;
					break;
				case NativeMouseEvents.Exited:
					mouseEvent = window.PointerExited;
					break;
				case NativeMouseEvents.Down:
					mouseEvent = window.PointerPressed;
					break;
				case NativeMouseEvents.Up:
					mouseEvent = window.PointerReleased;
					break;
				case NativeMouseEvents.Moved:
					mouseEvent = window.PointerMoved;
					break;
				case NativeMouseEvents.ScrollWheel:
					mouseEvent = window.PointerWheelChanged;
					break;
			}
			if (mouseEvent is null)
			{
				return 0; // unhandled
			}

			mouseEvent(window, BuildPointerArgs(*data));
			// always let the native side know about the mouse events, e.g. setting keyWindow, embedded native controls
			return 0;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	private static PointerEventArgs BuildPointerArgs(NativeMouseEventData data)
	{
		var position = new Point(data.X, data.Y);
		var pointerDevice = PointerDevice.For(data.PointerDeviceType);
		var properties = GetPointerProperties(data).SetUpdateKindFromPrevious(_previousProperties);

		var point = new PointerPoint(data.FrameId, data.Timestamp, pointerDevice, data.Pid, position, position, data.InContact, properties);
		var args = new PointerEventArgs(point, data.KeyModifiers);

		_previousPosition = position;
		_previousProperties = properties;

		return args;
	}

	private static PointerPointProperties GetPointerProperties(NativeMouseEventData data)
	{
		var properties = new PointerPointProperties()
		{
			IsInRange = true,
			IsPrimary = true,
			IsLeftButtonPressed = (data.MouseButtons & NSEventTypeLeftMouseDown) == NSEventTypeLeftMouseDown,
			IsRightButtonPressed = (data.MouseButtons & NSEventTypeRightMouseDown) == NSEventTypeRightMouseDown,
			IsMiddleButtonPressed = (data.MouseButtons & NSEventTypeOtherMouseDown) == NSEventTypeOtherMouseDown,
		};

		if (data.PointerDeviceType == PointerDeviceType.Pen)
		{
			properties.XTilt = data.TiltX;
			properties.YTilt = data.TiltY;
			properties.Pressure = data.Pressure;
		}

		if (data.EventType == NativeMouseEvents.ScrollWheel)
		{
			var y = data.ScrollingDeltaY;
			if (y == 0)
			{
				// Note: if X and Y are != 0, we should raise 2 events!
				properties.IsHorizontalMouseWheel = true;
				properties.MouseWheelDelta = data.ScrollingDeltaX;
			}
			else
			{
				properties.MouseWheelDelta = y;
			}
		}

		return properties;
	}

	public void ReleasePointerCapture() => LogNotSupported();
	public void ReleasePointerCapture(PointerIdentifier p) => LogNotSupported();
	public void SetPointerCapture() => LogNotSupported();
	public void SetPointerCapture(PointerIdentifier p) => LogNotSupported();

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on macOS.");
		}
	}

	// Window

	internal event EventHandler<CancelEventArgs>? Closing;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	// System.Boolean is not blittable / https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
	internal static int WindowShouldClose(nint handle)
	{
		try
		{
			var window = GetWindowHost(handle);
			var cancel = new CancelEventArgs();
			window?.Closing?.Invoke(window, cancel);
			return cancel.Cancel ? 0 : 1;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
		}
	}

	internal event EventHandler<EventArgs>? Closed;

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void WindowClose(nint handle)
	{
		try
		{
			var window = GetWindowHost(handle);
			if (window is not null)
			{
				// Stop the render thread before tearing down the native window / GRContext.
				window._metalRenderThread?.Dispose();
				window._metalRenderThread = null;
				Unregister(handle);
				window._nativeWindow.Destroyed();
				window.Closed?.Invoke(window, EventArgs.Empty);
			}
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	// DisplayInformation

	public MacOSDisplayInformationExtension? DisplayInformationExtension { get; set; }

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void ScreenChanged(nint handle, uint width, uint height, double scaleFactor)
	{
		if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSWindowHost).Log().Trace($"MacOSWindowHost.ScreenChanged window: {handle} size {width} x {height} @ {scaleFactor}x");
		}

		try
		{
			var window = GetWindowHost(handle);
			window?.DisplayInformationExtension?.Update(width, height, scaleFactor);
			window?.RasterizationScaleChanged?.Invoke(window, EventArgs.Empty);

			if (window?._metalRenderThread is { } renderThread)
			{
				// Moving between screens can change the refresh rate (e.g. 120Hz laptop -> 60Hz external).
				renderThread.UpdateTargetFps(ResolveTargetFps(NativeUno.uno_window_get_refresh_rate(handle)));
			}
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	internal static void ScreenParametersChanged(nint handle)
	{
		if (typeof(MacOSWindowHost).Log().IsEnabled(LogLevel.Trace))
		{
			typeof(MacOSWindowHost).Log().Trace($"MacOSWindowHost.ScreenParametersChanged window: {handle}");
		}

		try
		{
			var window = GetWindowHost(handle);
			window?._displayInformation.NotifyDpiChanged();
			window?.RasterizationScaleChanged?.Invoke(window, EventArgs.Empty);
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
		}
	}

	// --- Render thread ---

	/// <summary>
	/// Dedicated render thread for macOS Metal: acquires a drawable, draws the recorded
	/// SKPicture, flushes, then presents — all off the UI thread so a slow present / VSync
	/// wait never blocks input or layout. Mirrors the Win32 render thread and the iOS
	/// CADisplayLink render thread.
	/// </summary>
	/// <remarks>
	/// Frame requests are paced by a <see cref="FramePacer"/>, exactly as X11 paces its render
	/// thread. This is not an optimization: <c>nextDrawable</c> blocks for ~1s and then returns nil
	/// once the layer's pool is exhausted, so an unpaced loop that acquires as fast as it is
	/// signalled outruns the compositor and turns every frame into a one-second stall. Pacing keeps
	/// acquisitions at most one per refresh interval, which is the rate the compositor recycles at.
	/// A timer drives the pace rather than the display, so the render clock keeps ticking even when
	/// the display does not (occluded window, headless CI agent).
	/// </remarks>
	private sealed class MacOSRenderThread : IDisposable
	{
		/// <summary>
		/// Consecutive unpresentable frames tolerated at the normal pace before retries start
		/// backing off. Brief failures are expected (a zero-sized layer during window setup, a
		/// drawable still held across a resize) and should recover within a frame or two.
		/// </summary>
		private const int UnthrottledRetries = 3;

		private const int InitialBackoffMs = 16;
		private const int MaxBackoffMs = 1000;

		/// <summary>Minimum interval between "still cannot present" warnings.</summary>
		private const int FailureLogIntervalMs = 5000;

		/// <summary>A successful acquisition slower than this is worth reporting.</summary>
		private const int SlowAcquireMs = 100;

		private readonly Thread _thread;
		private readonly AutoResetEvent _frameSignal = new(false);
		private readonly ManualResetEventSlim _presentedEvent = new(false);
		private readonly ManualResetEventSlim _shutdown = new(false);
		private readonly FramePacer _framePacer;
		private readonly nint _windowHandle;
		private readonly GRContext _context;
		private readonly Action<double, double, nint> _drawFrame;
		private volatile bool _disposed;

		// Render-thread only.
		private int _consecutiveFailures;
		private long _firstFailureTimestamp;
		private long _lastFailureLogTimestamp;
		private long _lastAcquireLogTimestamp;

		internal MacOSRenderThread(nint windowHandle, GRContext context, Action<double, double, nint> drawFrame, double targetFps)
		{
			_windowHandle = windowHandle;
			_context = context;
			_drawFrame = drawFrame;
			_framePacer = new FramePacer(targetFps, SignalFrameDue);
			_thread = new Thread(RenderLoop) { Name = "Uno macOS Render Thread", IsBackground = true };
			_thread.Start();
		}

		/// <summary>
		/// Pacer callback: the frame deadline has arrived, so let the render loop run.
		/// </summary>
		/// <remarks>
		/// <see cref="Dispose"/> stops the pacer before disposing the events, but a timer callback
		/// already in flight can still land afterwards — swallow that rather than let it surface as
		/// an unhandled exception on a timer thread during window teardown.
		/// </remarks>
		private void SignalFrameDue()
		{
			try
			{
				_frameSignal.Set();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		/// <summary>
		/// Asks for a frame. Requests made within the same frame interval coalesce into one
		/// wake-up. Resets the present-completion event first so a <see cref="WaitForNextPresent"/>
		/// caller can never observe a previous present.
		/// </summary>
		internal void RequestFrame()
		{
			_presentedEvent.Reset();
			_framePacer.RequestFrame();
		}

		/// <summary>
		/// Retargets the pace, e.g. when the window moves to a screen with a different refresh rate.
		/// </summary>
		internal void UpdateTargetFps(double fps)
		{
			if (fps > 0)
			{
				_framePacer.UpdateTargetFps(fps);
			}
		}

		/// <summary>
		/// Blocks until the render thread finishes presenting the current frame, and returns
		/// <see langword="false"/> if the timeout elapsed first.
		/// </summary>
		/// <remarks>
		/// Currently unused. It mirrors the Win32 render-thread contract, where the UI thread waits for
		/// a present during a synchronous resize/show (Win32WindowWrapper.SynchronousRenderAndDraw).
		/// </remarks>
		internal bool WaitForNextPresent(TimeSpan timeout) => _presentedEvent.Wait(timeout);

		private void RenderLoop()
		{
			while (!_disposed)
			{
				_frameSignal.WaitOne();
				if (_disposed)
				{
					break;
				}

				_framePacer.OnFrameStart();

				var framePresented = false;
				try
				{
					// Timed: whether nextDrawable blocks (and for how long) on a given machine is the
					// single fact that separates "the render thread is stuck" from "the render thread is
					// idle and something else is slow". Nothing else records it.
					var acquireStart = Stopwatch.GetTimestamp();
					var acquired = NativeUno.uno_window_acquire_next_frame(_windowHandle, out var texture, out var width, out var height);
					ReportAcquire(acquired, ElapsedMsSince(acquireStart));

					if (acquired)
					{
						_drawFrame(width, height, texture);

						// submit: true commits the Metal command buffer before present (matches the iOS path).
						_context.Flush(submit: true);

						// Present the drawable; may block on VSync / drawable availability.
						NativeUno.uno_window_present_frame(_windowHandle);

						// Only a frame that actually reached the screen may release a waiter.
						_presentedEvent.Set();
						framePresented = true;
					}
				}
				catch (Exception ex)
				{
					// Intentionally broad: the loop must survive any per-frame Skia or interop failure.
					// Letting an exception escape would end the thread and stop rendering permanently.
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"macOS render thread error: {ex}");
					}

					// The drawable is only released by uno_window_present_frame, which we did not reach.
					// Drop it explicitly, otherwise repeated failures exhaust the layer's drawable pool
					// and every later nextDrawable call blocks then returns nil.
					NativeUno.uno_window_discard_frame(_windowHandle);
				}

				if (framePresented)
				{
					OnFramePresented();
				}
				else if (!_disposed)
				{
					RetryFrame();
				}
			}
		}

		private void OnFramePresented()
		{
			if (_consecutiveFailures == 0)
			{
				return;
			}

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info(
					$"macOS render thread presented a frame again after {_consecutiveFailures} failed " +
					$"attempt(s) over {ElapsedMsSince(_firstFailureTimestamp)}ms.");
			}

			_consecutiveFailures = 0;
			_lastFailureLogTimestamp = 0;
		}

		/// <summary>
		/// Re-arms a frame that could not be presented. The request must not be dropped:
		/// <see cref="CompositionTarget.RequestNewFrame"/> latches its render-requested flag and only
		/// clears it once a frame is actually drawn, so it will not signal us again on its own, and
		/// anything awaiting a render (RenderTargetBitmap jobs, composition animations) would hang.
		/// Retries are paced, and back off once failures persist so a window that can never present
		/// (minimized, or a compositor that stopped recycling drawables) does not spin the GPU.
		/// </summary>
		private void RetryFrame()
		{
			if (_consecutiveFailures == 0)
			{
				_firstFailureTimestamp = Stopwatch.GetTimestamp();
			}

			_consecutiveFailures++;
			ReportPersistentFailure();

			var backoff = BackoffDelayMs(_consecutiveFailures);
			if (backoff > 0 && _shutdown.Wait(backoff))
			{
				// Disposed while backing off.
				return;
			}

			_framePacer.RequestFrame();
		}

		/// <summary>
		/// Surfaces a drawable acquisition that failed, or succeeded but blocked. Both are reported the
		/// first time and then at most once per <see cref="FailureLogIntervalMs"/>, so a pathological
		/// agent shows up in the log immediately instead of only after several consecutive failures.
		/// </summary>
		private void ReportAcquire(bool acquired, long elapsedMs)
		{
			if (acquired && elapsedMs < SlowAcquireMs)
			{
				return;
			}

			if (!this.Log().IsEnabled(LogLevel.Warning))
			{
				return;
			}

			if (_lastAcquireLogTimestamp != 0 && ElapsedMsSince(_lastAcquireLogTimestamp) < FailureLogIntervalMs)
			{
				return;
			}

			_lastAcquireLogTimestamp = Stopwatch.GetTimestamp();
			this.Log().Warn(
				$"macOS drawable acquisition {(acquired ? "was slow" : "returned no drawable")} for window "
				+ $"{_windowHandle}: nextDrawable took {elapsedMs}ms.");
		}

		private void ReportPersistentFailure()
		{
			if (!this.Log().IsEnabled(LogLevel.Warning))
			{
				return;
			}

			if (_consecutiveFailures <= UnthrottledRetries)
			{
				// Still inside the tolerated window — a frame or two of failure is routine.
				return;
			}

			if (_consecutiveFailures > UnthrottledRetries + 1
				&& ElapsedMsSince(_lastFailureLogTimestamp) < FailureLogIntervalMs)
			{
				return;
			}

			_lastFailureLogTimestamp = Stopwatch.GetTimestamp();
			this.Log().Warn(
				$"macOS render thread could not present {_consecutiveFailures} consecutive frame(s) over " +
				$"{ElapsedMsSince(_firstFailureTimestamp)}ms for window {_windowHandle}: the layer vended no " +
				$"drawable. Retrying with backoff; rendering for this window is stalled until one is available.");
		}

		private static long ElapsedMsSince(long timestamp)
			=> timestamp == 0 ? 0 : (long)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;

		private static int BackoffDelayMs(int consecutiveFailures)
		{
			if (consecutiveFailures <= UnthrottledRetries)
			{
				// Still within the tolerated window: retry at the normal pace.
				return 0;
			}

			// Double from InitialBackoffMs, capped — the shift is bounded so it cannot overflow.
			var steps = Math.Min(consecutiveFailures - UnthrottledRetries - 1, 8);
			return Math.Min(MaxBackoffMs, InitialBackoffMs << steps);
		}

		/// <summary>
		/// Stops the render thread, waits for it to exit, then releases its synchronization
		/// primitives. The join is intentionally unbounded, matching the Win32 render thread: the
		/// loop only delays observing <see cref="_disposed"/> while a frame is in flight, and both
		/// <c>nextDrawable</c> and the present complete in bounded time (a vsync wait, or ~1s and a
		/// nil drawable when the window is occluded), so the thread always exits. A retry backoff
		/// waits on <see cref="_shutdown"/> rather than sleeping, so it is cut short here. The caller
		/// tears down the native window and the <see cref="GRContext"/> right after this returns, and
		/// the render thread is their sole other user, so it must be guaranteed stopped first.
		/// </summary>
		public void Dispose()
		{
			_disposed = true;
			_shutdown.Set();
			_frameSignal.Set();
			_thread.Join();

			_framePacer.Dispose();
			_frameSignal.Dispose();
			_presentedEvent.Dispose();
			_shutdown.Dispose();
		}
	}
}
