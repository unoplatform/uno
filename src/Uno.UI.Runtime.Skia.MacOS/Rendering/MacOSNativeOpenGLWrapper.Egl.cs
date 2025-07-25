// Copyright (c) 2006-2019 Stefanos Apostolopoulos for the Open Toolkit project.
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.UI.Runtime.Skia.MacOS;

internal partial class MacOSNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private const string libEGL = "libEGL.dylib";

	[DllImport(libEGL, EntryPoint = "eglGetDisplay")]
	public static extern IntPtr EglGetDisplay(IntPtr display_id);

	[DllImport(libEGL, EntryPoint = "eglInitialize")]
	private static extern bool EglInitialize(IntPtr dpy, out int major, out int minor);

	[DllImport(libEGL, EntryPoint = "eglChooseConfig")]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool EglChooseConfig(IntPtr dpy, int[] attrib_list, [In][Out] IntPtr[] configs, int config_size, out int num_config);

	[DllImport(libEGL, EntryPoint = "eglGetError")]
	private static extern ErrorCode EglGetError();

	[DllImport(libEGL, EntryPoint = "eglCreateContext")]
	private static extern IntPtr EglCreateContext(IntPtr dpy, IntPtr config, IntPtr share_context, int[] attrib_list);

	[DllImport(libEGL, EntryPoint = "eglCreatePbufferSurface")]
	private static extern IntPtr EglCreatePbufferSurface(IntPtr dpy, IntPtr config, int[] attrib_list);

	[DllImport(libEGL, EntryPoint = "eglDestroySurface")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool EglDestroySurface(IntPtr dpy, IntPtr surface);

	[DllImport(libEGL, EntryPoint = "eglDestroyContext")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool EglDestroyContext(IntPtr dpy, IntPtr ctx);

	[DllImport(libEGL, EntryPoint = "eglGetCurrentContext")]
	public static extern IntPtr EglGetCurrentContext();

	[DllImport(libEGL, EntryPoint = "eglGetCurrentDisplay")]
	public static extern IntPtr EglGetCurrentDisplay();

	[DllImport(libEGL, EntryPoint = "eglGetCurrentSurface")]
	public static extern IntPtr EglGetCurrentSurface(int readdraw);

	[DllImport(libEGL, EntryPoint = "eglMakeCurrent")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool EglMakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);

	private enum ErrorCode
	{
		SUCCESS = 0x3000,
		NOT_INITIALIZED,
		BAD_ACCESS,
		BAD_ALLOC,
		BAD_ATTRIBUTE,
		BAD_CONFIG,
		BAD_CONTEXT,
		BAD_CURRENT_SURFACE,
		BAD_DISPLAY,
		BAD_MATCH,
		BAD_NATIVE_PIXMAP,
		BAD_NATIVE_WINDOW,
		BAD_PARAMETER,
		BAD_SURFACE,
		CONTEXT_LOST
	}
}
