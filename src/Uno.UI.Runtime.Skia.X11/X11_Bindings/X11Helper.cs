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
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Windows.UI.ViewManagement;
using Microsoft.UI.Windowing;
using Windows.UI.Xaml;
using Uno.Disposables;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// This class includes missing bindings, atoms and other helper methods for X11.
/// </summary>
internal static partial class X11Helper
{
	private const string libX11 = "libX11.so.6";
	private const string libX11Randr = "libXrandr.so.2";
	private const string libXext = "libXext.so.6";

	public static readonly IntPtr CurrentTime = IntPtr.Zero;
	public static readonly IntPtr None = IntPtr.Zero;
	public static readonly IntPtr AnyPropertyType = IntPtr.Zero;
	public static readonly IntPtr PropertyNewValue = IntPtr.Zero;

	public const string RESOURCE_MANAGER = "RESOURCE_MANAGER";
	public const string WM_DELETE_WINDOW = "WM_DELETE_WINDOW";
	public const string _NET_ACTIVE_WINDOW = "_NET_ACTIVE_WINDOW";
	public const string _NET_WM_STATE = "_NET_WM_STATE";
	public const string _NET_WM_STATE_FULLSCREEN = "_NET_WM_STATE_FULLSCREEN";
	public const string _NET_WM_WINDOW_OPACITY = "_NET_WM_WINDOW_OPACITY";
	public const string _NET_CLIENT_LIST = "_NET_CLIENT_LIST";
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
	public const string XdndAware = "XdndAware";
	public const string XdndStatus = "XdndStatus";
	public const string XdndEnter = "XdndEnter";
	public const string XdndPosition = "XdndPosition";
	public const string XdndTypeList = "XdndTypeList";
	public const string XdndActionCopy = "XdndActionCopy";
	public const string XdndActionLink = "XdndActionLink";
	public const string XdndActionMove = "XdndActionMove";
	public const string XdndDrop = "XdndDrop";
	public const string XdndLeave = "XdndLeave";
	public const string XdndFinished = "XdndFinished";
	public const string XdndSelection = "XdndSelection";
	public const string XdndProxy = "XdndProxy";

	public const int ShapeSet = 0;
	public const int ShapeUnion = 1;
	public const int ShapeIntersect = 2;
	public const int ShapeSubtract = 3;
	public const int ShapeInvert = 4;
	public const int ShapeBounding = 0;
	public const int ShapeClip = 1;
	public const int ShapeInput = 2;
	public const int Unsorted = 0;

	public const int Success = 0;

	public const int POLLIN = 0x001; /* There is data to read.  */
	public const int POLLPRI = 0x002; /* There is urgent data to read.  */
	public const int POLLOUT = 0x004; /* Writing now will not block.  */

	public const IntPtr LONG_LENGTH = 0x7FFFFFFF;

	private static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, object> _displayToLock = new();

	// For some reason, using XLockDisplay and XLockDisplay seems to cause a deadlock,
	// specifically when using GLX. We use a managed locking implementation instead,
	// which might even be faster anyway depending on how slow socket I/O is.
	public static DisposableStruct<object> XLock(IntPtr display)
	{
		var @lock = _displayToLock.GetOrAdd(display, _ => new object());
		Monitor.Enter(@lock);
		return new DisposableStruct<object>(Monitor.Exit, @lock);
	}

	public static bool XamlRootHostFromApplicationView(ApplicationView view, [NotNullWhen(true)] out X11XamlRootHost? x11XamlRootHost)
	{
		// TODO: this is a ridiculous amount of indirection, find something better
		if (AppWindow.GetFromWindowId(view.WindowId) is { } appWindow &&
			Window.GetFromAppWindow(appWindow) is { } window &&
			X11XamlRootHost.GetHostFromWindow(window) is { } host)
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
			X11XamlRootHost.GetHostFromWindow(window) is { } host)
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
		using var lockDiposable = XLock(x11Window.Display);

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
		_ = XLib.XSendEvent(x11Window.Display, XLib.XDefaultRootWindow(x11Window.Display), false, (IntPtr)(XEventMask.SubstructureRedirectMask | XEventMask.SubstructureNotifyMask), ref xev);
		_ = XLib.XFlush(x11Window.Display);
	}

	public static void SetMotifWMDecorations(X11Window x11Window, bool on, IntPtr decorations)
		=> SetMotifWMHints(x11Window, on, decorations, null);

	public static void SetMotifWMFunctions(X11Window x11Window, bool on, IntPtr functions)
		=> SetMotifWMHints(x11Window, on, null, functions);

	// Sadly, there is no de jure standard for this. It's basically a set of hints used
	// in the old Motif WM. Other WMs started using it and it became a thing.
	// https://stackoverflow.com/a/13788970
	// https://www.opengroup.org/infosrv/openmotif/R2.1.30/motif/lib/Xm/MwmUtil.h
	// typedef struct
	// {
	// /* These correspond to XmRInt resources. (VendorSE.c) */
	// int	         flags;
	// int		 functions;
	// int		 decorations;
	// int		 input_mode;
	// int		 status;
	// } MotifWmHints;
	//
	// typedef MotifWmHints	MwmHints;
	//
	// /* bit definitions for MwmHints.flags */
	// #define MWM_HINTS_FUNCTIONS	(1L << 0)
	// #define MWM_HINTS_DECORATIONS	(1L << 1)
	// #define MWM_HINTS_INPUT_MODE	(1L << 2)
	// #define MWM_HINTS_STATUS	(1L << 3)
	//
	// /* bit definitions for MwmHints.functions */
	// #define MWM_FUNC_ALL		(1L << 0)
	// #define MWM_FUNC_RESIZE		(1L << 1)
	// #define MWM_FUNC_MOVE		(1L << 2)
	// #define MWM_FUNC_MINIMIZE	(1L << 3)
	// #define MWM_FUNC_MAXIMIZE	(1L << 4)
	// #define MWM_FUNC_CLOSE		(1L << 5)
	//
	// /* bit definitions for MwmHints.decorations */
	// #define MWM_DECOR_ALL		(1L << 0)
	// #define MWM_DECOR_BORDER	(1L << 1)
	// #define MWM_DECOR_RESIZEH	(1L << 2)
	// #define MWM_DECOR_TITLE		(1L << 3)
	// #define MWM_DECOR_MENU		(1L << 4)
	// #define MWM_DECOR_MINIMIZE	(1L << 5)
	// #define MWM_DECOR_MAXIMIZE	(1L << 6)
	private unsafe static void SetMotifWMHints(X11Window x11Window, bool on, IntPtr? decorations, IntPtr? functions)
	{
		using var lockDiposable = XLock(x11Window.Display);

		var hintsAtom = GetAtom(x11Window.Display, _MOTIF_WM_HINTS);
		_ = XLib.XGetWindowProperty(
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

		using var propDisposable = Disposable.Create(() =>
		{
			var _ = XLib.XFree(prop);
		});

		var arr = new IntPtr[5];
		if (actualType == None)
		{
			// the property wasn't set. Let's turn on everything by default.
			arr[0] |= (IntPtr)(MotifFlags.Decorations | MotifFlags.Functions);
			arr[1] |= (IntPtr)MotifFunctions.All;

			// Border doesn't seem to do anything except show the title bar even if Title is off,
			// so we turn it off.
			// arr[2] |= (IntPtr)MotifDecorations.All;
			arr[2] = ((int[])Enum.GetValuesAsUnderlyingType<MotifDecorations>()).Aggregate(0, (i1, i2) => i1 | i2);
			arr[2] &= ~(IntPtr)MotifDecorations.Border;
			arr[2] &= ~(IntPtr)MotifDecorations.All;
		}
		else
		{
			Debug.Assert(actual_format == 32 && nItems == 5);
			new Span<IntPtr>(prop.ToPointer(), 5).CopyTo(new Span<IntPtr>(arr, 0, 5));
		}

		if (functions is { } f)
		{
			arr[0] |= (IntPtr)MotifFlags.Functions;

			if ((arr[1] & (IntPtr)MotifDecorations.All) != 0)
			{
				// Remove the All function to be able to turn each function or or off individually.
				var allDecorations = (int[])Enum.GetValuesAsUnderlyingType<MotifFunctions>();
				arr[1] |= allDecorations.Aggregate(0, (i1, i2) => i1 | i2);
				arr[1] &= ~(IntPtr)MotifFunctions.All;
			}

			if (on)
			{
				arr[1] |= f;
			}
			else
			{
				arr[1] &= ~f;
			}
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

		_ = XLib.XChangeProperty(
			x11Window.Display,
			x11Window.Window,
			hintsAtom,
			hintsAtom,
			32,
			PropertyMode.Replace,
			arr,
			5);
		_ = XLib.XFlush(x11Window.Display);
	}

	private static Func<IntPtr, string, bool, IntPtr> _getAtom = Funcs.CreateMemoized<IntPtr, string, bool, IntPtr>(XLib.XInternAtom);
	public static IntPtr GetAtom(IntPtr display, string name, bool only_if_exists = false)
	{
		using var _ = XLock(display);
		return _getAtom(display, name, only_if_exists);
	}

	[LibraryImport("libc")]
	public unsafe static partial int poll(Pollfd* __fds, IntPtr __nfds, int __timeout);

	[LibraryImport(libX11)]
	public static partial int XPutImage(IntPtr display, IntPtr drawable, IntPtr gc, IntPtr image,
		int srcx, int srcy, int destx, int desty, uint width, uint height);

	[LibraryImport(libX11)]
	public static partial IntPtr XCreateGC(IntPtr display, IntPtr drawable, ulong valuemask, IntPtr values);

	[LibraryImport(libX11)]
	public static partial IntPtr XCreateImage(IntPtr display, IntPtr visual, uint depth, int format, int offset,
		IntPtr data, uint width, uint height, int bitmap_pad, int bytes_per_line);

	[LibraryImport(libX11)]
	public static partial int XClearWindow(IntPtr display, IntPtr window);

	[LibraryImport(libX11, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
	public static partial int XFetchName(IntPtr display, IntPtr window, out string name_return);

	[LibraryImport(libX11)]
	public unsafe static partial int XChangeWindowAttributes(
		IntPtr display, IntPtr window, IntPtr valuemask, XSetWindowAttributes* attributes);

	[LibraryImport(libX11)]
	public static partial int XReparentWindow(IntPtr display, IntPtr window, IntPtr parent, int x, int y);

	[LibraryImport(libX11)]
	public static partial int XRaiseWindow(IntPtr display, IntPtr window);

	[LibraryImport(libX11)]
	public static partial int XMoveWindow(IntPtr display, IntPtr window, int x, int y);

	[LibraryImport(libX11)]
	public static partial int XUnmapWindow(IntPtr display, IntPtr window);

	[LibraryImport(libX11)]
	public static partial int XPending(IntPtr display);

	[LibraryImport(libX11)]
	public static partial IntPtr XDefaultGC(IntPtr display, int screen_number);

	[LibraryImport(libX11)]
	public static partial int XInitThreads();

	[LibraryImport(libX11)]
	public static partial int XWidthMMOfScreen(IntPtr screen);

	[LibraryImport(libX11)]
	public static partial int XWidthOfScreen(IntPtr screen);

	[LibraryImport(libX11)]
	public static partial int XHeightMMOfScreen(IntPtr screen);

	[LibraryImport(libX11)]
	public static partial int XHeightOfScreen(IntPtr screen);

	[LibraryImport(libX11)]
	public static partial IntPtr XResourceManagerString(IntPtr display);

	[LibraryImport(libX11)]
	public static partial IntPtr XrmGetStringDatabase(IntPtr data);

	[LibraryImport(libX11)]
	public static partial void XrmDestroyDatabase(IntPtr database);

	[LibraryImport(libX11)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool XrmGetResource(IntPtr database, IntPtr str_name, IntPtr str_class, out IntPtr str_type_return, out XrmValue value_return);

	[LibraryImport(libX11)]
	public static partial IntPtr XCreateRegion();

	[LibraryImport(libX11)]
	public static partial int XDestroyRegion(IntPtr region);

	[LibraryImport(libX11)]
	public unsafe static partial int XUnionRectWithRegion(
		XRectangle* rectangle,
		IntPtr src_region,
		IntPtr dest_region_return
	);

	public unsafe static IntPtr CreateRegion(short x, short y, short w, short h)
	{
		IntPtr region = XCreateRegion();
		XRectangle rectangle;
		rectangle.X = x;
		rectangle.Y = y;
		rectangle.W = w;
		rectangle.H = h;
		var _ = XUnionRectWithRegion(&rectangle, region, region);

		return region;
	}

	[LibraryImport(libXext)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool XShapeQueryExtension(IntPtr dpy, out int event_base, out int error_base);

	[LibraryImport(libXext)]
	public static partial void XShapeCombineRegion(
		IntPtr display,
		IntPtr window,
		int dest_kind,
		int x_off,
		int y_off,
		IntPtr region,
		int op
	);

	[LibraryImport(libXext)]
	public unsafe static partial void XShapeCombineRectangles(
		IntPtr display,
		IntPtr window,
		int dest_kind,
		int x_off,
		int y_off,
		XRectangle* rectangles,
		int n_rects,
		int op,
		int ordering
	);

	[LibraryImport(libXext)]
	public unsafe static partial void XShapeCombineMask(
		IntPtr display,
		IntPtr window,
		int dest_kind,
		int x_off,
		int y_off,
		IntPtr src,
		int op
	);

	[LibraryImport(libX11)]
	public unsafe static partial IntPtr XCreateBitmapFromData(
		IntPtr display,
		IntPtr window,
		IntPtr data,
		uint width,
		uint height
	);

	[LibraryImport(libX11)]
	public unsafe static partial int XFreePixmap(
		IntPtr display,
		IntPtr pixmap
	);

	[LibraryImport(libX11Randr)]
	public unsafe static partial XRRScreenResources* XRRGetScreenResourcesCurrent(IntPtr dpy, IntPtr window);

	[LibraryImport(libX11Randr)]
	public unsafe static partial void XRRFreeScreenResources(XRRScreenResources* resources);

	[LibraryImport(libX11Randr)]
	public unsafe static partial XRROutputInfo* XRRGetOutputInfo(IntPtr dpy, IntPtr resources, IntPtr output);

	[LibraryImport(libX11Randr)]
	public unsafe static partial void XRRFreeOutputInfo(XRROutputInfo* outputInfo);

	[LibraryImport(libX11Randr)]
	public unsafe static partial XRRCrtcInfo* XRRGetCrtcInfo(IntPtr dpy, IntPtr resources, IntPtr crtc);

	[LibraryImport(libX11Randr)]
	public unsafe static partial void XRRFreeCrtcInfo(XRRCrtcInfo* crtcInfo);

	[LibraryImport(libX11Randr)]
	public unsafe static partial int XRRGetCrtcTransform(IntPtr dpy, IntPtr crtc, ref XRRCrtcTransformAttributes* attributes);

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

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct XrmValue
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public uint size;
		public IntPtr addr;
	}

	[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1815 // Override equals and operator equals on value types
	public struct Pollfd
#pragma warning restore CA1815 // Override equals and operator equals on value types
	{
		public int fd;
		public short events;
		public short revents;
	}
}
