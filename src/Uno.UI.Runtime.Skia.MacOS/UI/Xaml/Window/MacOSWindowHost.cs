using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Uno.UI.Hosting;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Media;
using Window = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowHost : IXamlRootHost, IUnoKeyboardInputSource, IUnoCorePointerInputSource
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private readonly MacOSWindowNative _nativeWindow;
	private readonly Window _winUIWindow;
	private readonly XamlRoot _xamlRoot;
	private readonly DisplayInformation _displayInformation;
	private readonly GRContext? _context;
	private SKBitmap? _bitmap;
	private SKSurface? _surface;
	private int _rowBytes;
	private bool _initializationCompleted;
	private string? _lastSvgClipPath;
	private Size _nativeWindowSize;

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

	private void MetalDraw(double nativeWidth, double nativeHeight, nint texture)
	{
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

		// we can't cache anything since the texture will be different on next calls
		GRBackendRenderTarget? target = null;
		SKSurface? surface = null;
		var nativeElementClipPath = ((CompositionTarget)RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(null, size =>
		{
			target = new GRBackendRenderTarget((int)size.Width, (int)size.Height, new GRMtlTextureInfo(texture));
			surface = SKSurface.Create(_context, target, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
			return surface.Canvas;
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

		_context?.Flush();
		target?.Dispose();
		surface?.Dispose();
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

		var nativeElementClipPath = ((CompositionTarget)RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(null, size =>
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

		NativeUno.uno_set_window_screen_change_callbacks(&ScreenChanged, &ScreenParametersChanged);
		ApiExtensibility.Register(typeof(IDisplayInformationExtension), o => new MacOSDisplayInformationExtension(o));
	}

	public UIElement? RootElement => _winUIWindow.RootElement;

	void IXamlRootHost.InvalidateRender()
	{
		if (!SkiaRenderHelper.CanRecordPicture(_winUIWindow.RootElement))
		{
			// Try again next tick
			_winUIWindow.RootElement?.XamlRoot?.QueueInvalidateRender();
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_nativeWindow.Handle} invalidated.");
		}

		XamlRootMap.GetRootForHost(this)?.VisualTree.ContentRoot.CompositionTarget.PaintFrame();

		NativeUno.uno_window_invalidate(_nativeWindow.Handle);
	}

	public static void Register(nint handle, XamlRoot xamlRoot, MacOSWindowHost host)
	{
		XamlRootMap.Register(xamlRoot, host);
		_windows.Add(handle, new WeakReference<MacOSWindowHost>(host));
	}

	private static void Unregister(nint handle) => _windows.Remove(handle);

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
		var window = GetWindowHost(handle);
		window?.MetalDraw(width, height, texture);
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void SoftDraw(nint handle, double width, double height, nint* data, int* rowBytes, int* size)
	{
		var window = GetWindowHost(handle);
		window?.SoftDraw(width, height, data, rowBytes, size);
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
			var root = window?._xamlRoot;
			return root is null || FocusManager.GetFocusedElement(root) == null ? 0 : 1;
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
			return 1;
		}
		catch (Exception e)
		{
			Application.Current.RaiseRecoverableUnhandledException(e);
			return 0;
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
}
