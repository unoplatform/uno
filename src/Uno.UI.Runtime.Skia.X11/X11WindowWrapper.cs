using System;
using System.Collections.Concurrent;
using System.Globalization;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11WindowWrapper : NativeWindowWrapperBase
{
	private readonly X11XamlRootHost _host;
	private readonly XamlRoot _xamlRoot;

	internal X11WindowWrapper(Window window, XamlRoot xamlRoot) : base(xamlRoot)
	{
		_xamlRoot = xamlRoot;

		_host = new X11XamlRootHost(window, xamlRoot, RaiseNativeSizeChanged, OnWindowClosing, OnNativeActivated, OnNativeVisibilityChanged);
	}

	public override string Title
	{
		get
		{
			using var _1 = X11Helper.XLock(_host.X11Window.Display);
			var @out = string.Empty;
			var _2 = XLib.XFetchName(_host.X11Window.Display, _host.X11Window.Window, ref @out);
			return @out;
		}
		set
		{
			using var _1 = X11Helper.XLock(_host.X11Window.Display);
			var _2 = XLib.XStoreName(_host.X11Window.Display, _host.X11Window.Window, value);
		}
	}

	public override object NativeWindow => _host.X11Window;

	private void RaiseNativeSizeChanged(Size newWindowSize)
	{
		var scale = ((IXamlRootHost)_host).RootElement?.XamlRoot is { } root
			? XamlRoot.GetDisplayInformation(root).RawPixelsPerViewPixel
			: 1;
		newWindowSize = new Size(newWindowSize.Width / scale, newWindowSize.Height / scale);
		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = new Rect(default, newWindowSize);
	}

	public override void Activate()
	{
		if (NativeWindow is X11Window x11Window)
		{
			using var _ = X11Helper.XLock(x11Window.Display);
			var _1 = XLib.XRaiseWindow(x11Window.Display, x11Window.Window);

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
			// var _2 = XLib.XSendEvent(x11Window.Display, XLib.XDefaultRootWindow(x11Window.Display), false, (IntPtr)(XEventMask.SubstructureRedirectMask | XEventMask.SubstructureNotifyMask), ref xev);
			// var _3 = XLib.XFlush(x11Window.Display);
		}
	}

	public override void Close()
	{
		var x11Window = (X11Window)NativeWindow;
		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Forcibly closing X11 window {x11Window.Display.ToString("X", CultureInfo.InvariantCulture)}, {x11Window.Window.ToString("X", CultureInfo.InvariantCulture)}");
		}
		using (X11Helper.XLock(x11Window.Display))
		{
			X11XamlRootHost.Close(x11Window);
			var _1 = XLib.XDestroyWindow(x11Window.Display, x11Window.Window);
			var _2 = XLib.XFlush(x11Window.Display);
		}

		RaiseClosed();
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

		var manager = SystemNavigationManagerPreview.GetForCurrentView();
		if (!manager.HasConfirmedClose)
		{
			if (!manager.RequestAppClose())
			{
				// App closing was prevented
				return;
			}
		}

		// All prerequisites passed, can safely close.
		Close();
	}

	private void OnNativeActivated(bool focused) => ActivationState = focused ? CoreWindowActivationState.PointerActivated : CoreWindowActivationState.Deactivated;

	private void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	protected override void ShowCore()
	{
		// The window needs to be mapped earlier because it's used in DisplayInformationExtension
		// if (NativeWindow is X11Window x11Window)
		// {
		// 	XLib.XMapWindow(x11Window.Display, x11Window.Window);
		// }
	}

	protected override IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter)
	{
		presenter.SetNative(new X11NativeOverlappedPresenter(_host.X11Window, this));
		return Disposable.Create(() => presenter.SetNative(null));
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		SetFullScreenMode(true);

		return Disposable.Create(() => SetFullScreenMode(false));
	}

	internal void SetFullScreenMode(bool on)
	{
		X11Helper.SetWMHints(
			_host.X11Window,
			X11Helper.GetAtom(_host.X11Window.Display, X11Helper._NET_WM_STATE),
			on ? 1 : 0,
			X11Helper.GetAtom(_host.X11Window.Display, X11Helper._NET_WM_STATE_FULLSCREEN));
	}
}
