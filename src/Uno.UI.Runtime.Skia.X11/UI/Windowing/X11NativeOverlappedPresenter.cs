using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI.Windowing.Native;
using Uno.Disposables;
using Uno.Foundation.Logging;
namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11NativeOverlappedPresenter(X11Window x11Window, X11WindowWrapper wrapper) : INativeOverlappedPresenter
{
	// EWMH has _NET_WM_ALLOWED_ACTIONS: https://specifications.freedesktop.org/wm-spec/wm-spec-1.3.html#idm45912237317440
	// but it turns out that these shouldn't be set by the client, but only read to see what actions are available.
	// Setting them doesn't really do anything.
	// There is also this from ICCCM, but I don't think people use this anymore:
	// https://specifications.freedesktop.org/wm-spec/wm-spec-1.3.html#NORESIZE
	// What works is using the Motif WM hints, which aren't standardized or documented anywhere
	// https://stackoverflow.com/a/13788970

	// This doesn't prevent resizing using xlib calls (e.g. XResizeWindow), so settings the size ApplicationView for example would still work.
	public void SetIsResizable(bool isResizable) => X11Helper.SetMotifWMFunctions(x11Window, isResizable, (IntPtr)MotifFunctions.Resize);

	public void SetIsModal(bool isModal)
	{
		// TODO: modal windows
	}

	// Making the window unminimizable removes the `-` button in the title bar and greys out the `Minimize` option if
	// you open the Menu, but the window will still be minimizable if you click on the window icon in the dock/task bar.
	// This is at least what happens on XFCE. Since these are just "hints", each WM can choose what it means to be "minimizable" differently.
	public void SetIsMinimizable(bool isMinimizable) => X11Helper.SetMotifWMFunctions(x11Window, isMinimizable, (IntPtr)MotifFunctions.Minimize);

	public void SetIsMaximizable(bool isMaximizable) => X11Helper.SetMotifWMFunctions(x11Window, isMaximizable, (IntPtr)MotifFunctions.Maximize);

	public void SetIsAlwaysOnTop(bool isAlwaysOnTop)
	{
		X11Helper.SetWMHints(
			x11Window,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE),
			isAlwaysOnTop ? 1 : 0,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_ABOVE));
	}

	public void Maximize()
	{
		X11Helper.SetWMHints(
			x11Window,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE),
			1,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_HORZ),
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_VERT));
	}

	public void Minimize(bool activateWindow)
	{
		using var lockDiposable = X11Helper.XLock(x11Window.Display);

		// Minimizing while in full screen could be buggy depending on the implementation
		// https://stackoverflow.com/questions/6381098/minimize-fullscreen-xlib-opengl-window
		wrapper.SetFullScreenMode(false);

		// XLib.XScreenNumberOfScreen(x11Window.Display, screen) is buggy. We use the default screen instead (which should be fine for 99% of cases)
		_ = XLib.XIconifyWindow(x11Window.Display, x11Window.Window, XLib.XDefaultScreen(x11Window.Display));
		_ = XLib.XFlush(x11Window.Display);
	}

	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		// Border doesn't seem to do anything except show the title bar even if !hasTitleBar, which is fine for now,
		// since it doesn't do anything on WinUI either.
		// X11Helper.SetMotifWMDecorations(x11Window, hasBorder, (IntPtr)MotifDecorations.Border);
		X11Helper.SetMotifWMDecorations(x11Window, hasTitleBar, (IntPtr)MotifDecorations.Title);
	}

	public void Restore(bool activateWindow)
	{
		// https://stackoverflow.com/a/30256233
		using var lockDiposable = X11Helper.XLock(x11Window.Display);

		var shouldActivate = activateWindow;
		shouldActivate |= GetWMState().Contains(X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_HIDDEN));
		if (!shouldActivate)
		{
			XWindowAttributes attributes = default;
			_ = XLib.XGetWindowAttributes(x11Window.Display, x11Window.Window, ref attributes);
			shouldActivate = attributes.map_state == MapState.IsUnmapped;
		}

		if (shouldActivate)
		{
			wrapper.Activate();
		}
	}

	public OverlappedPresenterState State
	{
		get
		{
			using var _1 = X11Helper.XLock(x11Window.Display);

			var minimized = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_HIDDEN);
			var maximizedHorizontal = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_HORZ);
			var maximizedVertical = X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE_MAXIMIZED_VERT);

			foreach (var atom in GetWMState())
			{
				if (atom == minimized)
				{
					return OverlappedPresenterState.Minimized;
				}
				else if (atom == maximizedHorizontal || atom == maximizedVertical) // maybe should we require both to be considered "maximized"?
				{
					return OverlappedPresenterState.Maximized;
				}
			}

			return OverlappedPresenterState.Restored;
		}
	}

	private unsafe IntPtr[] GetWMState()
	{
		using var _1 = X11Helper.XLock(x11Window.Display);

		var _2 = XLib.XGetWindowProperty(
			x11Window.Display,
			x11Window.Window,
			X11Helper.GetAtom(x11Window.Display, X11Helper._NET_WM_STATE),
			0,
			X11Helper.LONG_LENGTH,
			false,
			X11Helper.AnyPropertyType,
			out IntPtr actualType,
			out int actual_format,
			out IntPtr nItems,
			out _,
			out IntPtr prop);

		using var _3 = new DisposableStruct<IntPtr>(static p => { _ = XLib.XFree(p); }, prop);

		if (actualType == X11Helper.None)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Couldn't get {nameof(OverlappedPresenterState)}: {X11Helper._NET_WM_STATE} does not exist on the window. Make sure you use an EWMH-compliant WM.");
			}

			return Array.Empty<IntPtr>();
		}

		Debug.Assert(actual_format == 32);
		var span = new Span<IntPtr>(prop.ToPointer(), (int)nItems);

		return span.ToArray();
	}

	public unsafe void SetPreferredMinimumSize(int? preferredMinimumWidth, int? preferredMinimumHeight)
	{
		var minWidth = preferredMinimumWidth ?? 0;
		var minHeight = preferredMinimumHeight ?? 0;
		XSizeHints hints;
		XLib.XGetWMNormalHints(x11Window.Display, x11Window.Window, &hints, out _);
		hints.min_width = minWidth;
		hints.min_height = minHeight;
		hints.flags |= (int)XSizeHintsFlags.PMinSize;
		XLib.XSetWMNormalHints(x11Window.Display, x11Window.Window, ref hints);
	}

	public unsafe void SetPreferredMaximumSize(int? preferredMaximumWidth, int? preferredMaximumHeight)
	{
		var maxWidth = preferredMaximumWidth ?? int.MaxValue;
		var maxHeight = preferredMaximumHeight ?? int.MaxValue;
		XSizeHints hints;
		XLib.XGetWMNormalHints(x11Window.Display, x11Window.Window, &hints, out _);
		hints.max_width = maxWidth;
		hints.max_height = maxHeight;
		hints.flags |= (int)XSizeHintsFlags.PMaxSize;
		XLib.XSetWMNormalHints(x11Window.Display, x11Window.Window, ref hints);
	}
}
