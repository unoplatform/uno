using System.Collections.Concurrent;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Avalonia.X11;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11WindowWrapper : NativeWindowWrapperBase
{
	private X11XamlRootHost _host;
	private XamlRoot _xamlRoot;
	private static ConcurrentDictionary<Window, X11XamlRootHost> _windowToHost = new();

	internal X11WindowWrapper(Window window, XamlRoot xamlRoot)
	{
		_xamlRoot = xamlRoot;

		_host = new X11XamlRootHost(window, RaiseNativeSizeChanged, Close, OnNativeActivated, OnNativeVisibilityChanged);
		X11Manager.XamlRootMap.Register(xamlRoot, _host);

		_windowToHost[window] = _host;
		var host = X11XamlRootHost.GetXamlRootHostFromX11Window(_host.X11Window);
		host?.Closed.ContinueWith(task => _windowToHost.TryRemove(window, out _));
	}

	public static X11XamlRootHost? GetHostFromWindow(Window window)
		=> _windowToHost.TryGetValue(window, out var host) ? host : null;

	public override object NativeWindow => _host.X11Window;

	private void RaiseNativeSizeChanged(Size newWindowSize)
	{
		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = new Rect(default, newWindowSize);
	}

	// TODO: might need to send _NET_ACTIVE_WINDOW as well
	public override void Activate()
	{
		if (NativeWindow is X11Window x11Window)
		{
			using var _ = X11Helper.XLock(x11Window.Display);
			XLib.XRaiseWindow(x11Window.Display, x11Window.Window);
			XLib.XSetInputFocus(x11Window.Display, x11Window.Window, RevertTo.None,  X11Helper.CurrentTime);
		}
	}

	public override void Close()
	{
		if (NativeWindow is X11Window x11Window)
		{
			X11Manager.XamlRootMap.Unregister(_xamlRoot);
			X11XamlRootHost.Close(x11Window);
			using var _ = X11Helper.XLock(x11Window.Display);
			XLib.XDestroyWindow(x11Window.Display, x11Window.Window);
		}

		RaiseClosed();
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
}
