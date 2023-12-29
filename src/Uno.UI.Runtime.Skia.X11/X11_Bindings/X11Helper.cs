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
using System.Runtime.InteropServices;
using Avalonia.X11;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// This class includes missing bindings, atoms and other helper methods for X11.
/// </summary>
public static class X11Helper
{
	public const string WM_DELETE_WINDOW = "WM_DELETE_WINDOW";
	public const string _NET_WM_STATE = "_NET_WM_STATE";
	public const string _NET_WM_STATE_FULLSCREEN = "_NET_WM_STATE_FULLSCREEN";
	public const string TIMESTAMP = "TIMESTAMP";
	public const string MULTIPLE = "MULTIPLE";
	public const string TARGETS = "TARGETS";
	public const string CLIPBOARD = "CLIPBOARD";
	public const string XA_INTEGER = "XA_INTEGER";
	public const string XA_ATOM = "XA_ATOM";
	public const string UTF8_STRING = "UTF8_STRING";
	public const string XA_STRING = "XA_STRING";
	public const string INCR = "INCR";

	private static Func<IntPtr, string, bool, IntPtr> _getAtom = Funcs.CreateMemoized<IntPtr, string, bool, IntPtr>(XLib.XInternAtom);
	public static IntPtr GetAtom(IntPtr display, string name, bool only_if_exists = false) => _getAtom(display, name, only_if_exists);

	[DllImport("libX11.so.6")]
	public static extern int XPutImage(IntPtr display, IntPtr drawable, IntPtr gc, IntPtr image,
		int srcx, int srcy, int destx, int desty, uint width, uint height);

	[DllImport("libX11.so.6")]
	public static extern IntPtr XCreateImage(IntPtr display, IntPtr visual, uint depth, int format, int offset,
		IntPtr data, uint width, uint height, int bitmap_pad, int bytes_per_line);

	[DllImport("libX11.so.6")]
	public static extern int XPending(IntPtr display);

	[DllImport("libX11.so.6")]
	public static extern IntPtr XDefaultGC(IntPtr display, int screen_number);
}
