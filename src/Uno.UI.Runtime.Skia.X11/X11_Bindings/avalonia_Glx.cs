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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Avalonia.X11.Glx
{
    internal unsafe class GlxInterface
    {
        private const string libGL = "libGL.so.1";

		[DllImport(libGL)]
		public static extern bool glXQueryVersion(IntPtr dpy, out int maj, out int min);

		[DllImport(libGL)]
        public static extern bool glXQueryExtension(IntPtr dpy, out int errorBase, out int eventBase);

		[DllImport(libGL)]
		public static extern bool glXMakeContextCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);

		[DllImport(libGL)]
		public static extern bool glXMakeCurrent(IntPtr display, IntPtr drawable, IntPtr context);

		[DllImport(libGL)]
		public static extern IntPtr glXGetCurrentContext();

		[DllImport(libGL)]
		public static extern IntPtr glXGetCurrentDisplay();

		[DllImport(libGL)]
		public static extern IntPtr glXGetCurrentDrawable();
        
		[DllImport(libGL)]
		public static extern IntPtr glXGetCurrentReadDrawable();
        
		[DllImport(libGL)]
		public static extern IntPtr glXCreatePbuffer(IntPtr dpy, IntPtr fbc, int[] attrib_list);
        
		[DllImport(libGL)]
		public static extern IntPtr glXDestroyPbuffer(IntPtr dpy, IntPtr fb);
        
		[DllImport(libGL)]
		public static extern XVisualInfo* glXChooseVisual(IntPtr dpy, int screen, int[] attribList);

		[DllImport(libGL)]
		public static extern IntPtr glXCreateContext(IntPtr dpy,  XVisualInfo* vis,  IntPtr shareList,  bool direct);

		[DllImport(libGL)]
		public static extern IntPtr glXCreateNewContext(IntPtr dpy, IntPtr config, int renderType, IntPtr shareList, int direct);

		[DllImport(libGL)]
        public static extern IntPtr glXCreateContextAttribsARB(IntPtr dpy, IntPtr fbconfig, IntPtr shareList,
            bool direct, int[] attribs);
        
        [DllImport(libGL)]
        public static extern IntPtr glXGetProcAddress(string buffer);
        
		[DllImport(libGL)]
		public static extern void glXDestroyContext(IntPtr dpy, IntPtr ctx);
        
		[DllImport(libGL)]
		public static extern IntPtr* glXChooseFBConfig(IntPtr dpy,  int screen,  int[] attrib_list,  out int nelements);
        
        public IntPtr* glXChooseFBConfig(IntPtr dpy, int screen, IEnumerable<int> attribs, out int nelements)
        {
            var arr = attribs.Concat(new[]{0}).ToArray();
            return glXChooseFBConfig(dpy, screen, arr, out nelements);
        }
        
		[DllImport(libGL)]
		public static extern XVisualInfo * glXGetVisualFromFBConfig(IntPtr dpy,  IntPtr config);
        
		[DllImport(libGL)]
		public static extern int glXGetFBConfigAttrib(IntPtr dpy, IntPtr config, int attribute, out int value);
        
		[DllImport(libGL)]
		public static extern void glXSwapBuffers(IntPtr dpy,  IntPtr drawable);
        
		[DllImport(libGL)]
		public static extern void glXWaitX();
        
		[DllImport(libGL)]
		public static extern void glXWaitGL();

		[DllImport(libGL)]
		public static extern int glGetError();
        
		[DllImport(libGL)]
		public static extern IntPtr glXQueryExtensionsString(IntPtr display, int screen);

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
            return s?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim()).ToArray();
        }
    }
}
