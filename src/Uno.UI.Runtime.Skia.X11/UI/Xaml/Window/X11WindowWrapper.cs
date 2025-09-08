using System;
using System.Globalization;
using System.Threading;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11WindowWrapper : NativeWindowWrapperBase
{
	private readonly X11XamlRootHost _host;
	private readonly XamlRoot _xamlRoot;

	internal X11WindowWrapper(Window window, XamlRoot xamlRoot) : base(window, xamlRoot)
	{
		_xamlRoot = xamlRoot;

		_host = new X11XamlRootHost(this, window, xamlRoot, UpdatePositionAndSize, OnWindowClosing, OnNativeActivated, OnNativeVisibilityChanged);
		UpdatePositionAndSize(); // set initial values

		RasterizationScale = (float)XamlRoot.GetDisplayInformation(_xamlRoot).RawPixelsPerViewPixel;
	}

	public override string Title
	{
		get
		{
			using var lockDiposable = X11Helper.XLock(_host.RootX11Window.Display);
			var @out = string.Empty;
			_ = XLib.XFetchName(_host.RootX11Window.Display, _host.RootX11Window.Window, ref @out);
			return @out;
		}
		set
		{
			using var lockDiposable = X11Helper.XLock(_host.RootX11Window.Display);
			_ = XLib.XStoreName(_host.RootX11Window.Display, _host.RootX11Window.Window, value);
		}
	}

	public override object NativeWindow => new X11NativeWindow(_host.RootX11Window.Window);

	internal protected override void Activate()
	{
		var x11Window = _host.RootX11Window;
		using var lockDiposable = X11Helper.XLock(x11Window.Display);
		_ = XLib.XRaiseWindow(x11Window.Display, x11Window.Window);
		_ = XLib.XFlush(x11Window.Display); // Important! Otherwise X commands will sit waiting to be flushed, and since the window is not activated, there are no new X commands being sent to force a flush.

		// We could send _NET_ACTIVE_WINDOW as well, although it doesn't seem to be needed (and only works with EWMH-compliant WMs)
		// XClientMessageEvent xclient = default;
		// xclient.send_event = 1;
		// xclient.type = XEventName.ClientMessage;
		// xclient.window = x11Window.Window;
		// xclient.message_type = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_ACTIVE_WINDOW);
		// xclient.format = 32;
		// xclient.ptr1 = 1;
		// xclient.ptr2 = X11Helper.CurrentTime;
		//
		// XEvent xev = default;
		// xev.ClientMessageEvent = xclient;
		// _ = XLib.XSendEvent(x11Window.Display, XLib.XDefaultRootWindow(x11Window.Display), false, (IntPtr)(XEventMask.SubstructureRedirectMask | XEventMask.SubstructureNotifyMask), ref xev);
		// _ = XLib.XFlush(x11Window.Display);
	}

	protected override void CloseCore()
	{
		var x11Window = _host.RootX11Window;
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Forcibly closing X11 window {x11Window.Display.ToString("X", CultureInfo.InvariantCulture)}, {x11Window.Window.ToString("X", CultureInfo.InvariantCulture)}");
		}
		using (X11Helper.XLock(x11Window.Display))
		{
			X11XamlRootHost.Close(x11Window);
		}
	}

	public override void ExtendContentIntoTitleBar(bool extend)
	{
		base.ExtendContentIntoTitleBar(extend);
		_host.ExtendContentIntoTitleBar(extend);
	}

	private void OnWindowClosing()
	{
		var closingArgs = RaiseClosing();
		if (closingArgs.Cancel)
		{
			return;
		}

		// All prerequisites passed, can safely close.
		Close();
	}

	private void OnNativeActivated(bool focused) => ActivationState = focused ? CoreWindowActivationState.PointerActivated : CoreWindowActivationState.Deactivated;

	private void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	protected override void ShowCore()
	{
		using var lockDiposable = X11Helper.XLock(_host.RootX11Window.Display);
		using var lockDiposable2 = X11Helper.XLock(_host.TopX11Window.Display);
		_ = XLib.XMapWindow(_host.RootX11Window.Display, _host.RootX11Window.Window);
		_ = XLib.XMapWindow(_host.TopX11Window.Display, _host.TopX11Window.Window);
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new X11NativeOverlappedPresenter(_host.RootX11Window, this));
		return Disposable.Create(() => presenter.SetNative(null));
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		if (WasShown)
		{
			SetFullScreenMode(true);
		}

		return Disposable.Create(() =>
		{
			if (WasShown)
			{
				SetFullScreenMode(false);
			}
		});
	}

	public override void Move(PointInt32 position)
	{
		var display = _host.RootX11Window.Display;
		var window = _host.RootX11Window.Window;
		using var lockDiposable = X11Helper.XLock(display);

		_ = X11Helper.XMoveWindow(display, window, position.X, position.Y);
		XLib.XSync(display, false);
	}

	public override void Resize(SizeInt32 size)
	{
		var display = _host.RootX11Window.Display;
		var window = _host.RootX11Window.Window;
		using var lockDiposable = X11Helper.XLock(display);

		// If the window manager adds decorations, usually that is implemented by wrapping
		// the window in another slightly bigger window that includes the decorations. In that case,
		// XGetWindowAttributes will give us x and y offsets relative to this slightly bigger window,
		// not relative to the root window.
		_ = XLib.XQueryTree(display, window, out var root, out var parent, out var children, out _);
		_ = XLib.XQueryTree(display, parent, out _, out var parentParent, out var children2, out _);
		_ = XLib.XFree(children);
		_ = XLib.XFree(children2);

		var windowToResize = parentParent == root ? parent : window;
		_ = XLib.XResizeWindow(display, windowToResize, size.Width, size.Height);
		XLib.XSync(display, false);
	}

	private void UpdatePositionAndSize()
	{
		var display = _host.RootX11Window.Display;
		var window = _host.RootX11Window.Window;
		using var xLock = X11Helper.XLock(display);

		// If the window manager adds decorations, usually that is implemented by wrapping
		// the window in another slightly bigger window that includes the decorations. In that case,
		// XGetWindowAttributes will give us x and y offsets relative to this slightly bigger window,
		// not relative to the root window.
		_ = XLib.XQueryTree(display, window, out var root, out var parent, out var children, out _);
		_ = XLib.XQueryTree(display, parent, out _, out var parentParent, out var children2, out _);
		_ = XLib.XFree(children);
		_ = XLib.XFree(children2);

		var windowToRead = parentParent == root ? parent : window;
		XWindowAttributes windowAttrs = default;
		_ = XLib.XGetWindowAttributes(display, windowToRead, ref windowAttrs);
		_ = XLib.XTranslateCoordinates(display, windowToRead, root, 0, 0, out var rootx, out var rooty, out _);

		Position = new PointInt32 { X = rootx, Y = rooty };
		Size = new SizeInt32 { Width = windowAttrs.width, Height = windowAttrs.height };

		XWindowAttributes windowAttrs2 = default;
		_ = XLib.XGetWindowAttributes(display, window, ref windowAttrs2);

		var scale = _xamlRoot.RasterizationScale;
		var newWindowSize = new Size(windowAttrs2.width / scale, windowAttrs2.height / scale);
		var bounds = new Rect(default, newWindowSize);
		SetBoundsAndVisibleBounds(bounds, bounds);

		// copy the root window dimensions to the top window
		_ = XLib.XResizeWindow(display, _host.TopX11Window.Window, windowAttrs2.width, windowAttrs2.height);
	}

	internal void SetFullScreenMode(bool on)
	{
		if (WasShown)
		{
			X11Helper.SetWMHints(
				_host.RootX11Window,
				X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper._NET_WM_STATE),
				on ? 1 : 0,
				X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper._NET_WM_STATE_FULLSCREEN));
			_ = XLib.XSync(_host.RootX11Window.Display, false);
		}
	}
}
