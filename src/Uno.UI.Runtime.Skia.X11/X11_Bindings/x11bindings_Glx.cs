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

// https://github.com/AvaloniaUI/Avalonia/blob/5fa3ffaeab7e5cd2662ef02d03e34b9d4cb1a489/src/Avalonia.X11/Glx/Glx.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Uno.WinUI.Runtime.Skia.X11
{
	internal unsafe partial class GlxInterface
	{
		private const string libGL = "libGL.so.1";
		private static readonly char[] _separators = { ',', ' ' };
		private static readonly int[] _null = { 0 };

		[LibraryImport(libGL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool glXQueryVersion(IntPtr dpy, out int maj, out int min);

		[LibraryImport(libGL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool glXQueryExtension(IntPtr dpy, out int errorBase, out int eventBase);

		[LibraryImport(libGL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool glXMakeContextCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);

		[LibraryImport(libGL)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static partial bool glXMakeCurrent(IntPtr display, IntPtr drawable, IntPtr context);

		[LibraryImport(libGL)]
		public static partial IntPtr glXGetCurrentContext();

		[LibraryImport(libGL)]
		public static partial IntPtr glXGetCurrentDisplay();

		[LibraryImport(libGL)]
		public static partial IntPtr glXGetCurrentDrawable();

		[LibraryImport(libGL)]
		public static partial IntPtr glXGetCurrentReadDrawable();

		[LibraryImport(libGL)]
		public static partial IntPtr glXCreatePbuffer(IntPtr dpy, IntPtr fbc, int[] attrib_list);

		[LibraryImport(libGL)]
		public static partial IntPtr glXDestroyPbuffer(IntPtr dpy, IntPtr fb);

		[LibraryImport(libGL)]
		public static partial XVisualInfo* glXChooseVisual(IntPtr dpy, int screen, int[] attribList);

		[LibraryImport(libGL)]
		public static partial IntPtr glXCreateContext(IntPtr dpy, XVisualInfo* vis, IntPtr shareList, [MarshalAs(UnmanagedType.Bool)] bool direct);

		[LibraryImport(libGL)]
		public static partial IntPtr glXCreateNewContext(IntPtr dpy, IntPtr config, int renderType, IntPtr shareList, int direct);

		[LibraryImport(libGL)]
		public static partial IntPtr glXCreateContextAttribsARB(IntPtr dpy, IntPtr fbconfig, IntPtr shareList,
			[MarshalAs(UnmanagedType.Bool)] bool direct, int[] attribs);

		[LibraryImport(libGL, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
		public static partial IntPtr glXGetProcAddress(string buffer);

		[LibraryImport(libGL)]
		public static partial void glXDestroyContext(IntPtr dpy, IntPtr ctx);

		[LibraryImport(libGL)]
		public static partial IntPtr* glXChooseFBConfig(IntPtr dpy, int screen, int[] attrib_list, out int nelements);

		public IntPtr* glXChooseFBConfig(IntPtr dpy, int screen, IEnumerable<int> attribs, out int nelements)
		{
			var arr = attribs.Concat(_null).ToArray();
			return glXChooseFBConfig(dpy, screen, arr, out nelements);
		}

		[LibraryImport(libGL)]
		public static partial XVisualInfo* glXGetVisualFromFBConfig(IntPtr dpy, IntPtr config);

		[LibraryImport(libGL)]
		public static partial int glXGetFBConfigAttrib(IntPtr dpy, IntPtr config, int attribute, out int value);

		[LibraryImport(libGL)]
		public static partial void glXSwapBuffers(IntPtr dpy, IntPtr drawable);

		[LibraryImport(libGL)]
		public static partial void glXWaitX();

		[LibraryImport(libGL)]
		public static partial void glXWaitGL();

		[LibraryImport(libGL)]
		public static partial int glGetError();

		[LibraryImport(libGL)]
		public static partial IntPtr glXQueryExtensionsString(IntPtr display, int screen);

		// Ignores egl functions.
		// On some Linux systems, glXGetProcAddress will return valid pointers for even EGL functions.
		// This makes Skia try to load some data from EGL,
		// which can then cause segmentation faults because they return garbage.
		public static IntPtr SafeGetProcAddress(string proc)
		{
			if (proc.StartsWith("egl", StringComparison.InvariantCulture))
			{
				return IntPtr.Zero;
			}

			return glXGetProcAddress(proc);
		}

		public string[]? GetExtensions(IntPtr display)
		{
			var s = Marshal.PtrToStringAnsi(glXQueryExtensionsString(display, 0));
			return s?.Split(_separators, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Trim()).ToArray();
		}
	}
}
