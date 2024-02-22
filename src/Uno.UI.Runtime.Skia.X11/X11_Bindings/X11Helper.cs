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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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
	public const string _NET_WM_STATE = "_NET_WM_STATE";
	public const string _NET_WM_STATE_FULLSCREEN = "_NET_WM_STATE_FULLSCREEN";
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

	// Only change the visibility of this method if absolutely necessary. Instead, use XLock()
	[DllImport(libX11Randr)]
	private static extern void XLockDisplay(IntPtr display);

	[DllImport(libX11Randr)]
	private static extern void XUnlockDisplay(IntPtr display);

	[DllImport(libX11)]
	public static extern int XWidthMMOfScreen(IntPtr screen);

	public static IDisposable XLock(IntPtr display)
	{
		XLockDisplay(display);
		return Disposable.Create(() => XUnlockDisplay(display));
	}

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
}
