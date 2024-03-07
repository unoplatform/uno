// Thanks to the X11.NET project for making most bindings easily grabbable.
// Copyright (c) 2018 Andrew Newlands
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// This class includes missing bindings, atoms and other helper methods for X11.
/// </summary>
internal static class X11Helper
{
	private const string libX11 = "libX11.so.6";
	private const string libX11Randr = "libXrandr.so.2";

	public static readonly IntPtr CurrentTime = IntPtr.Zero;
	public static readonly IntPtr None = IntPtr.Zero;
	public static readonly IntPtr AnyPropertyType = IntPtr.Zero;
	public static readonly IntPtr PropertyNewValue = IntPtr.Zero;

	public const string WM_DELETE_WINDOW = "WM_DELETE_WINDOW";
	public const string _NET_ACTIVE_WINDOW = "_NET_ACTIVE_WINDOW";
	public const string _NET_WM_STATE = "_NET_WM_STATE";
	public const string _NET_WM_STATE_FULLSCREEN = "_NET_WM_STATE_FULLSCREEN";
	public const string _NET_WM_STATE_ABOVE = "_NET_WM_STATE_ABOVE";
	public const string _NET_WM_STATE_HIDDEN = "_NET_WM_STATE_HIDDEN";
	public const string _NET_WM_STATE_MAXIMIZED_HORZ = "_NET_WM_STATE_MAXIMIZED_HORZ";
	public const string _NET_WM_STATE_MAXIMIZED_VERT = "_NET_WM_STATE_MAXIMIZED_VERT";
	public const string _NET_WM_ALLOWED_ACTIONS = "_NET_WM_ALLOWED_ACTIONS";
	public const string _NET_WM_ACTION_MOVE = "_NET_WM_ACTION_MOVE";
	public const string _NET_WM_ACTION_RESIZE = "_NET_WM_ACTION_RESIZE";
	public const string _NET_WM_ACTION_MINIMIZE = "_NET_WM_ACTION_MINIMIZE";
	public const string _NET_WM_ACTION_MAXIMIZE_HORZ = "_NET_WM_ACTION_MAXIMIZE_HORZ";
	public const string _NET_WM_ACTION_MAXIMIZE_VERT = "_NET_WM_ACTION_MAXIMIZE_VERT";
	public const string _NET_WM_ICON = "_NET_WM_ICON";
	public const string _MOTIF_WM_HINTS = "_MOTIF_WM_HINTS";
	public const string TIMESTAMP = "TIMESTAMP";
	public const string MULTIPLE = "MULTIPLE";
	public const string TARGETS = "TARGETS";
	public const string CLIPBOARD = "CLIPBOARD";
	public const string PRIMARY = "PRIMARY";
	public const string XA_INTEGER = "INTEGER";
	public const string XA_ATOM = "ATOM";
	public const string XA_CARDINAL = "CARDINAL";
	public const string ATOM_PAIR = "ATOM_PAIR";
	public const string INCR = "INCR";

	public const IntPtr LONG_LENGTH = 0x7FFFFFFF;

	private static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, object> _displayToLock = new();

	// For some reason, using XLockDisplay and XLockDisplay seems to cause a deadlock,
	// specifically when using GLX. We use a managed locking implementation instead,
	// which might even be faster anyway depending on how slow socket I/O is.
	public static IDisposable XLock(IntPtr display)
	{
		var @lock = _displayToLock.GetOrAdd(display, _ => new object());
		Monitor.Enter(@lock);
		return new LockDisposable(@lock);
	}

	public static bool XamlRootHostFromApplicationView(ApplicationView view, [NotNullWhen(true)] out X11XamlRootHost? x11XamlRootHost)
	{
		// TODO: this is a ridiculous amount of indirection, find something better
		if (AppWindow.GetFromWindowId(view.WindowId) is { } appWindow &&
			Window.GetFromAppWindow(appWindow) is { } window &&
			X11WindowWrapper.GetHostFromWindow(window) is { } host)
		{
			x11XamlRootHost = host;
			return true;
		}

		x11XamlRootHost = null;
		return false;
	}

	public static bool XamlRootHostFromDisplayInformation(Windows.Graphics.Display.DisplayInformation displayInformation, [NotNullWhen(true)] out X11XamlRootHost? x11XamlRootHost)
	{
		// TODO: this is a ridiculous amount of indirection, find something better
		if (AppWindow.GetFromWindowId(displayInformation.WindowId) is { } appWindow &&
			Window.GetFromAppWindow(appWindow) is { } window &&
			X11WindowWrapper.GetHostFromWindow(window) is { } host)
		{
			x11XamlRootHost = host;
			return true;
		}

		x11XamlRootHost = null;
		return false;
	}

	public static void SetWMHints(X11Window x11Window, IntPtr message_type, IntPtr ptr1)
		=> SetWMHints(x11Window, message_type, ptr1, 0, 0, 0, 0);

	public static void SetWMHints(X11Window x11Window, IntPtr message_type, IntPtr ptr1, IntPtr ptr2)
		=> SetWMHints(x11Window, message_type, ptr1, ptr2, 0, 0, 0);

	public static void SetWMHints(X11Window x11Window, IntPtr message_type, IntPtr ptr1, IntPtr ptr2, IntPtr ptr3)
		=> SetWMHints(x11Window, message_type, ptr1, ptr2, ptr3, 0, 0);

	public static void SetWMHints(X11Window x11Window, IntPtr message_type, IntPtr ptr1, IntPtr ptr2, IntPtr ptr3, IntPtr ptr4)
		=> SetWMHints(x11Window, message_type, ptr1, ptr2, ptr3, ptr4, 0);

	public static void SetWMHints(X11Window x11Window, IntPtr message_type, IntPtr ptr1, IntPtr ptr2, IntPtr ptr3, IntPtr ptr4, IntPtr ptr5)
	{
		using var _1 = XLock(x11Window.Display);

		// https://stackoverflow.com/a/28396773
		XClientMessageEvent xclient = default;
		xclient.send_event = 1;
		xclient.type = XEventName.ClientMessage;
		xclient.window = x11Window.Window;
		xclient.message_type = message_type;
		xclient.format = 32;
		xclient.ptr1 = ptr1;
		xclient.ptr2 = ptr2;
		xclient.ptr3 = ptr3;
		xclient.ptr4 = ptr4;
		xclient.ptr5 = ptr5;

		XEvent xev = default;
		xev.ClientMessageEvent = xclient;
		var _2 = XLib.XSendEvent(x11Window.Display, XLib.XDefaultRootWindow(x11Window.Display), false, (IntPtr)(XEventMask.SubstructureRedirectMask | XEventMask.SubstructureNotifyMask), ref xev);
		var _3 = XLib.XFlush(x11Window.Display);
	}

	public static void SetMotifWMDecorations(X11Window x11Window, bool on, IntPtr decorations)
		=> SetMotifWMHints(x11Window, on, decorations, null);

	public static void SetMotifWMFunctions(X11Window x11Window, bool on, IntPtr functions)
		=> SetMotifWMHints(x11Window, on, null, functions);

	// Sadly, there is no de jure standard for this. It's basically a set of hints used
	// in the old Motif WM. Other WMs started using it and it became a thing.
	// https://stackoverflow.com/a/13788970
	private unsafe static void SetMotifWMHints(X11Window x11Window, bool on, IntPtr? decorations, IntPtr? functions)
	{
		using var _1 = XLock(x11Window.Display);

		var hintsAtom = GetAtom(x11Window.Display, _MOTIF_WM_HINTS);
		var _2 = XLib.XGetWindowProperty(
			x11Window.Display,
			x11Window.Window,
			hintsAtom,
			0,
			LONG_LENGTH,
			false,
			AnyPropertyType,
			out IntPtr actualType,
			out int actual_format,
			out IntPtr nItems,
			out _,
			out IntPtr prop);

		using var _3 = Disposable.Create(() => XLib.XFree(prop));

		var arr = new IntPtr[5];
		if (actualType != None)
		{
			Debug.Assert(actual_format == 32 && nItems == 5);
			new Span<IntPtr>(prop.ToPointer(), 5).CopyTo(new Span<IntPtr>(arr, 0, 5));
		}

		if (decorations is { } d)
		{
			arr[0] |= (IntPtr)MotifFlags.Decorations;

			if ((arr[2] & (IntPtr)MotifDecorations.All) != 0)
			{
				// Remove the All decoration to be able to turn each decoration or or off individually.
				var allDecorations = (int[])Enum.GetValuesAsUnderlyingType<MotifDecorations>();
				arr[2] |= allDecorations.Aggregate(0, (i1, i2) => i1 | i2);
				arr[2] &= ~(IntPtr)MotifDecorations.All;
			}

			if (on)
			{
				arr[2] |= d;
			}
			else
			{
				arr[2] &= ~d;
			}
		}

		if (functions is { } f)
		{
			arr[0] |= (IntPtr)MotifFlags.Functions;
			if (on)
			{
				arr[1] |= f;
			}
			else
			{
				arr[1] &= ~f;
			}
		}

		var _4 = XLib.XChangeProperty(
			x11Window.Display,
			x11Window.Window,
			hintsAtom,
			hintsAtom,
			32,
			PropertyMode.Replace,
			arr,
			5);
		var _5 = XLib.XFlush(x11Window.Display);
	}

	private static Func<IntPtr, string, bool, IntPtr> _getAtom = Funcs.CreateMemoized<IntPtr, string, bool, IntPtr>(XLib.XInternAtom);
	public static IntPtr GetAtom(IntPtr display, string name, bool only_if_exists = false) => _getAtom(display, name, only_if_exists);

	[DllImport(libX11)]
	public static extern int XPutImage(IntPtr display, IntPtr drawable, IntPtr gc, IntPtr image,
		int srcx, int srcy, int destx, int desty, uint width, uint height);

	[DllImport(libX11)]
	public static extern IntPtr XCreateImage(IntPtr display, IntPtr visual, uint depth, int format, int offset,
		IntPtr data, uint width, uint height, int bitmap_pad, int bytes_per_line);

	[DllImport(libX11)]
	public static extern int XPending(IntPtr display);

	[DllImport(libX11)]
	public static extern IntPtr XDefaultGC(IntPtr display, int screen_number);

	[DllImport(libX11)]
	public static extern int XInitThreads();

	[DllImport(libX11)]
	public static extern int XWidthMMOfScreen(IntPtr screen);

	[DllImport(libX11)]
	public static extern int XWidthOfScreen(IntPtr screen);

	[DllImport(libX11)]
	public static extern int XHeightMMOfScreen(IntPtr screen);

	[DllImport(libX11)]
	public static extern int XHeightOfScreen(IntPtr screen);

	[DllImport(libX11Randr)]
	public unsafe static extern XRRScreenResources* XRRGetScreenResourcesCurrent(IntPtr dpy, IntPtr window);

	[DllImport(libX11Randr)]
	public unsafe static extern void XRRFreeScreenResources(XRRScreenResources* resources);

	[DllImport(libX11Randr)]
	public unsafe static extern XRROutputInfo* XRRGetOutputInfo(IntPtr dpy, IntPtr resources, IntPtr output);

	[DllImport(libX11Randr)]
	public unsafe static extern void XRRFreeOutputInfo(XRROutputInfo* outputInfo);

	[DllImport(libX11Randr)]
	public unsafe static extern XRRCrtcInfo* XRRGetCrtcInfo(IntPtr dpy, IntPtr resources, IntPtr crtc);

	[DllImport(libX11Randr)]
	public unsafe static extern void XRRFreeCrtcInfo(XRRCrtcInfo* crtcInfo);

	[DllImport(libX11Randr)]
	public unsafe static extern int XRRGetCrtcTransform(IntPtr dpy, IntPtr crtc, ref XRRCrtcTransformAttributes* attributes);

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct XRROutputInfo
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public IntPtr timestamp;
		public IntPtr crtc;
		public IntPtr name;
		public int nameLen;
		public IntPtr mm_width;
		public IntPtr mm_height;
		public ushort connection;
		public ushort subpixel_order;
		public int ncrtc;
		public IntPtr crtcs;
		public int nclone;
		public IntPtr clones;
		public int nmode;
		public int npreferred;
		public IntPtr modes;
	}

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct XRRScreenResources
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public IntPtr timestamp;
		public IntPtr configTimestamp;
		public int ncrtc;
		public IntPtr crtcs;
		public int noutput;
		public IntPtr outputs;
		public int nmode;
		public IntPtr modes;
	}

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct XRRModeInfo
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public IntPtr id;
		public uint width;
		public uint height;
		public IntPtr dotClock;
		public uint hSyncStart;
		public uint hSyncEnd;
		public uint hTotal;
		public uint hSkew;
		public uint vSyncStart;
		public uint vSyncEnd;
		public uint vTotal;
		public IntPtr name;
		public uint nameLength;
		public IntPtr modeFlags;
	}

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct XRRCrtcInfo
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public IntPtr timestamp;
		public int x, y;
		public uint width, height;
		public IntPtr mode;
		public ushort rotation;
		public int noutput;
		public IntPtr outputs;
		public ushort rotations;
		public int npossible;
		public IntPtr possible;
	}

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct XRRCrtcTransformAttributes
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public XTransform pendingTransform;
		public IntPtr pendingFilter;
		public int pendingNparams;
		public IntPtr pendingParams;
		public XTransform currentTransform;
		public IntPtr currentFilter;
		public int currentNparams;
		public IntPtr currentParams;
	}

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct XTransform
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public int _1_1;
		public int _1_2;
		public int _1_3;
		public int _2_1;
		public int _2_2;
		public int _2_3;
		public int _3_1;
		public int _3_2;
		public int _3_3;
	}

	private struct LockDisposable(object @lock): IDisposable
	{
		public void Dispose() => Monitor.Exit(@lock);
	}
}
