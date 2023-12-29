using System;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Avalonia.X11;
using Microsoft.UI.Windowing;

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal class X11ApplicationViewExtension : IApplicationViewExtension
	{
		private readonly ApplicationView _owner;

		public X11ApplicationViewExtension(object owner)
		{
			_owner = (ApplicationView)owner;
		}

		public void ExitFullScreenMode() => TrySetFullScreenMode(false);

		public bool TryEnterFullScreenMode() => TrySetFullScreenMode(true);

		private bool TrySetFullScreenMode(bool on)
		{
			// TODO: this is a ridiculous amount of indirection, find something better
			if (AppWindow.GetFromWindowId(_owner.WindowId) is { } appWindow &&
				Window.GetFromAppWindow(appWindow) is { } window &&
				X11WindowWrapper.GetHostFromWindow(window) is { } host)
			{
				IntPtr wm_state = X11Helper.GetAtom(host.X11Window.Display, X11Helper._NET_WM_STATE);
				IntPtr wm_fullscreen = X11Helper.GetAtom(host.X11Window.Display, X11Helper._NET_WM_STATE_FULLSCREEN);

				if (wm_state == /* None */ IntPtr.Zero || wm_fullscreen == /* None */ IntPtr.Zero)
				{
					return false;
				}

				// https://stackoverflow.com/a/28396773
				XClientMessageEvent xclient = default;
				xclient.type = XEventName.ClientMessage;
				xclient.window = host.X11Window.Window;
				xclient.message_type = wm_state;
				xclient.format = 32;
				xclient.ptr1 = on ? 1 : 0;
				xclient.ptr2 = wm_fullscreen;
				xclient.ptr3 = 0;
				xclient.ptr4 = 0;
				xclient.ptr5 = 0;

				XEvent xev = default;
				xev.ClientMessageEvent = xclient;
				XLib.XSendEvent(host.X11Window.Display, XLib.XDefaultRootWindow(host.X11Window.Display), false, (IntPtr)(XEventMask.SubstructureRedirectMask | XEventMask.SubstructureNotifyMask), ref xev);
				XLib.XFlush(host.X11Window.Display);
			}

			return true;
		}

		public bool TryResizeView(Size size)
		{
			// TODO: this is a ridiculous amount of indirection, find something better
			if (AppWindow.GetFromWindowId(_owner.WindowId) is { } appWindow &&
				Window.GetFromAppWindow(appWindow) is { } window &&
				X11WindowWrapper.GetHostFromWindow(window) is { } host)
			{
				XLib.XResizeWindow(host.X11Window.Display, host.X11Window.Window, (int)size.Width, (int)size.Height);
			}
			return false;
		}
	}
}
