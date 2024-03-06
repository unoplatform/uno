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

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable CommentTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Uno.WinUI.Runtime.Skia.X11
{
	public unsafe static class XLib
	{
		private const string libX11 = "libX11.so.6";
		private const string libX11Randr = "libXrandr.so.2";

		[DllImport(libX11)]
		public static extern IntPtr XOpenDisplay(IntPtr display);

		[DllImport(libX11)]
		public static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width,
			int height, int border_width, IntPtr border, IntPtr background);

		[DllImport(libX11)]
		public static extern IntPtr XCreateWindow(IntPtr display, IntPtr parent, int x, int y, int width, int height,
			int border_width, int depth, int xclass, IntPtr visual, UIntPtr valuemask,
			ref XSetWindowAttributes attributes);

		[DllImport(libX11)]
		public static extern int XMapWindow(IntPtr display, IntPtr window);

		[DllImport(libX11)]
		public static extern IntPtr XRootWindow(IntPtr display, int screen_number);
		[DllImport(libX11)]
		public static extern IntPtr XDefaultRootWindow(IntPtr display);

		[DllImport(libX11)]
		public static extern IntPtr XNextEvent(IntPtr display, out XEvent xevent);

		[DllImport(libX11)]
		public static extern int XPending(IntPtr diplay);

		[DllImport(libX11)]
		public static extern IntPtr XSelectInput(IntPtr display, IntPtr window, IntPtr mask);

		[DllImport(libX11)]
		public static extern int XDestroyWindow(IntPtr display, IntPtr window);

		[DllImport(libX11)]
		public static extern int XConnectionNumber(IntPtr display);

		[DllImport(libX11)]
		public static extern int XResizeWindow(IntPtr display, IntPtr window, int width, int height);

		[DllImport(libX11)]
		public static extern int XGetWindowAttributes(IntPtr display, IntPtr window, ref XWindowAttributes attributes);

		[DllImport(libX11)]
		public static extern int XFlush(IntPtr display);

		[DllImport(libX11)]
		public static extern int XStoreName(IntPtr display, IntPtr window, string window_name);

		[DllImport(libX11)]
		public static extern int XSendEvent(IntPtr display, IntPtr window, bool propagate, IntPtr event_mask,
			ref XEvent send_event);

		[DllImport(libX11)]
		public static extern int XQueryTree(IntPtr display, IntPtr window, out IntPtr root_return,
			out IntPtr parent_return, out IntPtr children_return, out int nchildren_return);

		[DllImport(libX11)]
		public static extern int XFree(IntPtr data);

		[DllImport(libX11)]
		public static extern int XRaiseWindow(IntPtr display, IntPtr window);

		[DllImport(libX11)]
		public static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

		[DllImport(libX11)]
		public static extern IntPtr XGetAtomName(IntPtr display, IntPtr atom);

		public static string GetAtomName(IntPtr display, IntPtr atom)
		{
			var ptr = XGetAtomName(display, atom);
			if (ptr == IntPtr.Zero)
				return null;
			var s = Marshal.PtrToStringAnsi(ptr);
			var _ = XFree(ptr);
			return s;
		}

		[DllImport(libX11)]
		public static extern int XSetWMProtocols(IntPtr display, IntPtr window, IntPtr[] protocols, int count);

		[DllImport(libX11)]
		public static extern bool XTranslateCoordinates(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x,
			int src_y, out int intdest_x_return, out int dest_y_return, out IntPtr child_return);

		[DllImport(libX11)]
		public static extern int XDefaultScreen(IntPtr display);

		[DllImport(libX11)]
		public static extern int XScreenNumberOfScreen(IntPtr display, IntPtr Screen);

		[DllImport(libX11)]
		public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
			int format, PropertyMode mode, byte[] data, int nelements);

		[DllImport(libX11)]
		public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
			int format, PropertyMode mode, IntPtr[] data, int nelements);

		[DllImport(libX11)]
		public static extern int XChangeProperty(IntPtr display, IntPtr window, IntPtr property, IntPtr type,
			int format, PropertyMode mode, IntPtr atoms, int nelements);

		[DllImport(libX11)]
		public static extern int XDeleteProperty(IntPtr display, IntPtr window, IntPtr property);

		[DllImport(libX11)]
		public static extern int XGetWindowProperty(IntPtr display, IntPtr window, IntPtr atom, IntPtr long_offset,
			IntPtr long_length, bool delete, IntPtr req_type, out IntPtr actual_type, out int actual_format,
			out IntPtr nitems, out IntPtr bytes_after, out IntPtr prop);

		[DllImport(libX11)]
		public static extern int XSetInputFocus(IntPtr display, IntPtr window, RevertTo revert_to, IntPtr time);

		[DllImport(libX11)]
		public static extern int XDefineCursor(IntPtr display, IntPtr window, IntPtr cursor);

		[DllImport(libX11)]
		public static extern int XUndefineCursor(IntPtr display, IntPtr window);

		[DllImport(libX11)]
		public static extern int XFreeCursor(IntPtr display, IntPtr cursor);

		[DllImport(libX11)]
		public static extern IntPtr XCreateFontCursor(IntPtr display, CursorFontShape shape);

		[DllImport(libX11)]
		public static extern IntPtr XWhitePixel(IntPtr display, int screen_no);

		[DllImport(libX11)]
		public static extern IntPtr XBlackPixel(IntPtr display, int screen_no);

		[DllImport(libX11)]
		public static extern IntPtr XSetErrorHandler(XErrorHandler error_handler);

		[DllImport(libX11)]
		public static extern int XConvertSelection(IntPtr display, IntPtr selection, IntPtr target, IntPtr property,
			IntPtr requestor, IntPtr time);

		[DllImport(libX11)]
		public static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr selection);

		[DllImport(libX11)]
		public static extern int XSetSelectionOwner(IntPtr display, IntPtr selection, IntPtr owner, IntPtr time);

		[DllImport(libX11)]
		public static extern int XDestroyImage(IntPtr image);

		[DllImport(libX11)]
		public static extern int XSync(IntPtr display, bool discard);

		[DllImport(libX11)]
		public static extern IntPtr XCreateColormap(IntPtr display, IntPtr window, IntPtr visual, int create);

		[DllImport(libX11)]
		public static extern int XLookupString(ref XKeyEvent xevent, byte* buffer, int num_bytes, out nint keysym, IntPtr composeStatus);

		[DllImport(libX11)]
		public static extern IntPtr XSetLocaleModifiers(string modifiers);

		[DllImport(libX11Randr)]
		public static extern int XRRQueryExtension(IntPtr dpy,
			out int event_base_return,
			out int error_base_return);

		[DllImport(libX11Randr)]
		public static extern int XRRQueryVersion(IntPtr dpy,
			out int major_version_return,
			out int minor_version_return);

		[DllImport(libX11Randr)]
		public static extern XRRMonitorInfo*
			XRRGetMonitors(IntPtr dpy, IntPtr window, bool get_active, out int nmonitors);

		[DllImport(libX11Randr)]
		public static extern IntPtr* XRRListOutputProperties(IntPtr dpy, IntPtr output, out int count);

		[DllImport(libX11Randr)]
		public static extern int XRRGetOutputProperty(IntPtr dpy, IntPtr output, IntPtr atom, int offset, int length, bool _delete, bool pending, IntPtr req_type, out IntPtr actual_type, out int actual_format, out int nitems, out long bytes_after, out IntPtr prop);
	}
}
