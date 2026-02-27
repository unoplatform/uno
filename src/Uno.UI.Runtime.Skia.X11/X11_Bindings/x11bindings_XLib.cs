// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
// All Rights Reserved
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

// https://github.com/AvaloniaUI/Avalonia/blob/5fa3ffaeab7e5cd2662ef02d03e34b9d4cb1a489/src/Avalonia.X11/XLib.cs

#nullable disable
#pragma warning disable CS8500
#pragma warning disable CS0649

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable CommentTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Uno.WinUI.Runtime.Skia.X11
{
	public unsafe static partial class XLib
	{
		private const string libX11 = "libX11.so.6";
		private const string libX11Randr = "libXrandr.so.2";
		internal const string libXInput = "libXi.so.6";
		internal const string libXfixes = "libXfixes.so.3";

		[LibraryImport(libX11)]
		public static partial IntPtr XOpenDisplay(IntPtr display);

		[LibraryImport(libX11)]
		public static partial int XCloseDisplay(IntPtr display);

		[LibraryImport(libX11)]
		public static partial int XDefaultDepth(IntPtr display, int screen);

		[LibraryImport(libX11)]
		public static partial int XMatchVisualInfo(IntPtr display, int screen, int depth, int klass, out XVisualInfo info);

		[LibraryImport(libX11)]
		public static partial IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width,
			int height, int border_width, IntPtr border, IntPtr background);

		[LibraryImport(libX11)]
		public static partial IntPtr XCreateWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height,
			int border_width, int depth, int xclass, IntPtr visual, UIntPtr valuemask,
			ref XSetWindowAttributes attributes);

		[LibraryImport(libX11)]
		public static partial int XMapWindow(IntPtr display, IntPtr window);

		[DllImport(libX11)]
		public static extern int XUnmapWindow(IntPtr display, IntPtr window);

		[LibraryImport(libX11)]
		public static partial IntPtr XRootWindow(IntPtr display, int screen_number);
		[LibraryImport(libX11)]
		public static partial IntPtr XDefaultRootWindow(IntPtr display);

		[LibraryImport(libX11)]
		public static partial IntPtr XNextEvent(IntPtr display, out XEvent xevent);

		[LibraryImport(libX11)]
		public static partial int XPending(IntPtr diplay);

		[LibraryImport(libX11)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static partial bool XQueryExtension(IntPtr display, [MarshalAs(UnmanagedType.LPStr)] string name,
			out int majorOpcode, out int firstEvent, out int firstError);

		[LibraryImport(libXInput)]
		internal static partial int XIQueryVersion(IntPtr dpy, ref int major, ref int minor);

		[LibraryImport(libXInput)]
		internal static unsafe partial XIDeviceInfo* XIQueryDevice(IntPtr dpy, int deviceid, out int ndevices_return);

		[LibraryImport(libXInput)]
		internal static unsafe partial void XIFreeDeviceInfo(XIDeviceInfo* info);

		internal static bool XIMaskIsSet(void* ptr, int shift) =>
			(((byte*)ptr)[shift >> 3] & (1 << (shift & 7))) != 0;

		[LibraryImport(libXInput)]
		internal static unsafe partial int XISelectEvents(
			IntPtr dpy,
			IntPtr win,
			XIEventMask* masks,
			int num_masks
		);

		[DllImport(libX11)]
		internal static extern unsafe bool XGetEventData(IntPtr display, XGenericEventCookie* cookie);

		[LibraryImport(libX11)]
		internal static unsafe partial void XFreeEventData(IntPtr display, void* cookie);

		[LibraryImport(libX11)]
		public static partial IntPtr XSelectInput(IntPtr display, IntPtr window, IntPtr mask);

		[LibraryImport(libX11)]
		public static partial int XDestroyWindow(IntPtr display, IntPtr window);

		[LibraryImport(libX11)]
		public static partial int XConnectionNumber(IntPtr display);

		[LibraryImport(libX11)]
		public static partial int XResizeWindow(IntPtr display, IntPtr window, int width, int height);

		[LibraryImport(libX11)]
		public static partial int XGetWindowAttributes(IntPtr display, IntPtr window, ref XWindowAttributes attributes);

		[LibraryImport(libX11)]
		public static partial int XFlush(IntPtr display);

		[LibraryImport(libX11, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
		public static partial int XStoreName(IntPtr display, IntPtr window, string window_name);

		[LibraryImport(libX11, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
		public static partial int XFetchName(IntPtr display, IntPtr window, ref string window_name);

		[LibraryImport(libX11)]
		public static partial void XSetWMNormalHints(IntPtr display, IntPtr window, ref XSizeHints hints);

		[LibraryImport(libX11)]
		public static partial int XSendEvent(IntPtr display, IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool propagate, IntPtr event_mask,
			ref XEvent send_event);

		[LibraryImport(libX11)]
		public static partial int XQueryTree(IntPtr display, IntPtr window, out IntPtr root_return,
			out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

		[LibraryImport(libX11)]
		public static partial int XFree(IntPtr data);

		[LibraryImport(libX11)]
		public static partial int XRaiseWindow(IntPtr display, IntPtr window);

		[LibraryImport(libX11, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
		public static partial IntPtr XInternAtom(IntPtr display, string atom_name, [MarshalAs(UnmanagedType.Bool)] bool only_if_exists);

		[LibraryImport(libXfixes)]
		public static partial void XFixesSelectSelectionInput(IntPtr display, IntPtr window, IntPtr selection, IntPtr event_mask);

		[LibraryImport(libX11)]
		public static partial IntPtr XGetAtomName(IntPtr display, IntPtr atom);

		public static string GetAtomName(IntPtr display, IntPtr atom)
		{
			var ptr = XGetAtomName(display, atom);
			if (ptr == IntPtr.Zero)
				return null;
			var s = Marshal.PtrToStringAnsi(ptr);
			var _ = XFree(ptr);
			return s;
		}

		[LibraryImport(libX11)]
		public static partial int XSetWMProtocols(IntPtr display, IntPtr window, IntPtr[] protocols, int count);

		[LibraryImport(libX11)]
		public static partial int XIconifyWindow(IntPtr display, IntPtr window, int screen_number);

		[LibraryImport(libX11)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool XTranslateCoordinates(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x,
			int src_y, out int intdest_x_return, out int dest_y_return, out IntPtr child_return);

		[LibraryImport(libX11)]
		public static partial int XDefaultScreen(IntPtr display);

		[LibraryImport(libX11)]
		public static partial int XScreenNumberOfScreen(IntPtr display, IntPtr Screen);

		[LibraryImport(libX11)]
		public static partial int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
			int format, PropertyMode mode, byte[] data, int nelements);

		[LibraryImport(libX11)]
		public static partial int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
			int format, PropertyMode mode, IntPtr[] data, int nelements);

		[LibraryImport(libX11)]
		public static partial int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
			int format, PropertyMode mode, IntPtr atoms, int nelements);

		[LibraryImport(libX11)]
		public static partial int XDeleteProperty(IntPtr display, IntPtr window, IntPtr property);

		[LibraryImport(libX11)]
		public static partial int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr atom, IntPtr long_offset,
			IntPtr long_length, [MarshalAs(UnmanagedType.Bool)] bool delete, IntPtr req_type, out IntPtr actual_type, out int actual_format,
			out IntPtr nitems, out IntPtr bytes_after, out IntPtr prop);

		[LibraryImport(libX11)]
		public static partial int XSetInputFocus(IntPtr display, IntPtr window, RevertTo revert_to, IntPtr time);

		[LibraryImport(libX11)]
		public static partial int XDefineCursor(IntPtr display, IntPtr window, IntPtr cursor);

		[LibraryImport(libX11)]
		public static partial int XUndefineCursor(IntPtr display, IntPtr window);

		[LibraryImport(libX11)]
		public static partial int XFreeCursor(IntPtr display, IntPtr cursor);

		[LibraryImport(libX11)]
		public static partial IntPtr XCreateFontCursor(IntPtr display, CursorFontShape shape);

		[LibraryImport(libX11)]
		public static partial IntPtr XWhitePixel(IntPtr display, int screen_no);

		[LibraryImport(libX11)]
		public static partial IntPtr XBlackPixel(IntPtr display, int screen_no);

		[LibraryImport(libX11)]
		public static partial IntPtr XSetErrorHandler(XErrorHandler error_handler);

		[LibraryImport(libX11)]
		public static partial int XConvertSelection(IntPtr display, IntPtr selection, IntPtr target, IntPtr property,
			IntPtr requestor, IntPtr time);

		[LibraryImport(libX11)]
		public static partial IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

		[LibraryImport(libX11)]
		public static partial int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

		[LibraryImport(libX11)]
		public static partial int XDestroyImage(IntPtr image);

		[LibraryImport(libX11)]
		public static partial int XSync(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool discard);

		[LibraryImport(libX11)]
		public static partial IntPtr XCreateColormap(IntPtr display, IntPtr window, IntPtr visual, int create);

		[LibraryImport(libX11)]
		public static partial int XLookupString(ref XKeyEvent xevent, byte* buffer, int num_bytes, out nint keysym, IntPtr composeStatus);

		[LibraryImport(libX11, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
		public static partial IntPtr XSetLocaleModifiers(string modifiers);

		[LibraryImport(libX11Randr)]
		public static partial int XRRQueryExtension(IntPtr dpy,
			out int event_base_return,
			out int error_base_return);

		[LibraryImport(libX11Randr)]
		public static partial int XRRQueryVersion(IntPtr dpy,
			out int major_version_return,
			out int minor_version_return);

		[LibraryImport(libX11Randr)]
		public static partial XRRMonitorInfo*
			XRRGetMonitors(IntPtr dpy, IntPtr window, [MarshalAs(UnmanagedType.Bool)] bool get_active, out int nmonitors);

		[LibraryImport(libX11Randr)]
		public static partial IntPtr* XRRListOutputProperties(IntPtr dpy, IntPtr output, out int count);

		[LibraryImport(libX11Randr)]
		public static partial int XRRGetOutputProperty(IntPtr dpy, IntPtr output, IntPtr atom, int offset, int length, [MarshalAs(UnmanagedType.Bool)] bool _delete, [MarshalAs(UnmanagedType.Bool)] bool pending, IntPtr req_type, out IntPtr actual_type, out int actual_format, out int nitems, out long bytes_after, out IntPtr prop);
	}
}
