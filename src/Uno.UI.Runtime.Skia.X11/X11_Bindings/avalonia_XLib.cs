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

namespace Avalonia.X11
{
	public unsafe static class XLib
	{
		private const string libX11 = "libX11.so.6";

		[DllImport(libX11)]
		public static extern IntPtr XOpenDisplay(IntPtr display);

		[DllImport(libX11)]
		public static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, int width,
			int height, int border_width, IntPtr border, IntPtr background);

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
			XFree(ptr);
			return s;
		}

		[DllImport(libX11)]
		public static extern int XSetWMProtocols(IntPtr display, IntPtr window, IntPtr[] protocols, int count);


		[DllImport(libX11)]
		public static extern int XDefaultScreen(IntPtr display);

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
	}
}
