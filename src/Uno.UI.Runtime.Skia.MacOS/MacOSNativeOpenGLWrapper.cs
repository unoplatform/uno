using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.UI.Runtime.Skia.MacOS;

// ANGLE implements EGL 1.5
// https://registry.khronos.org/EGL/api/EGL/egl.h
// Permalink: https://github.com/KhronosGroup/EGL-Registry/blob/29c4314e0ef04c730992d295f91b76635019fbba/api/EGL/egl.h

internal class MacOSNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private const string libEGL = "libEGL.dylib";

	private const int EGL_DEFAULT_DISPLAY = 0;
	private const int EGL_NO_CONTEXT = 0;
	private const int EGL_ALPHA_SIZE = 0x3021;
	private const int EGL_BLUE_SIZE = 0x3022;
	private const int EGL_GREEN_SIZE = 0x3023;
	private const int EGL_RED_SIZE = 0x3024;
	private const int EGL_DEPTH_SIZE = 0x3025;
	private const int EGL_STENCIL_SIZE = 0x3026;
	private const int EGL_NONE = 0x3038;
	private const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
	private const int EGL_DRAW = 0x3059;
	private const int EGL_READ = 0x305A;

	private IntPtr _eglDisplay;
	private IntPtr _glContext;
	private IntPtr _pBufferSurface;

	public void CreateContext(UIElement element)
	{
		_eglDisplay = EglGetDisplay(EGL_DEFAULT_DISPLAY);
		EglInitialize(_eglDisplay, out var major, out var minor);
		int[] pi32ConfigAttribs =
		{
			EGL_RED_SIZE, 8,
			EGL_GREEN_SIZE, 8,
			EGL_BLUE_SIZE, 8,
			EGL_ALPHA_SIZE, 8,
			EGL_DEPTH_SIZE, 8,
			EGL_STENCIL_SIZE, 1,
			EGL_NONE
		};

		var configs = new IntPtr[1];
		var success = EglChooseConfig(_eglDisplay, pi32ConfigAttribs, configs, configs.Length, out var numConfig);

		if (!success)
		{
			throw new InvalidOperationException($"{nameof(EglChooseConfig)} failed: {EglGetError()}");
		}

		// ANGLE implements GLES 3
		_glContext = EglCreateContext(_eglDisplay, configs[0], EGL_NO_CONTEXT, new[] { EGL_CONTEXT_CLIENT_VERSION, 3, EGL_NONE });
		if (_glContext == IntPtr.Zero)
		{
			throw new InvalidOperationException($"EGL context creation failed: {EglGetError()}");
		}

		_pBufferSurface = EglCreatePbufferSurface(_eglDisplay, configs[0], new[] { EGL_NONE });
		if (_pBufferSurface == IntPtr.Zero)
		{
			throw new InvalidOperationException($"EGL pbuffer surface creation failed: {EglGetError()}");
		}
	}
	public object CreateGLSilkNETHandle() => GL.GetApi(new MacOSAngleNativeContext());
	public void DestroyContext()
	{
		if (_eglDisplay != IntPtr.Zero && _pBufferSurface != IntPtr.Zero)
		{
			EglDestroySurface(_eglDisplay, _pBufferSurface);
		}
		if (_eglDisplay != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			EglDestroyContext(_eglDisplay, _glContext);
		}

		_pBufferSurface = IntPtr.Zero;
		_glContext = IntPtr.Zero;
		_eglDisplay = IntPtr.Zero;
	}
	public IDisposable MakeCurrent()
	{
		var glContext = EglGetCurrentContext();
		var display = EglGetCurrentDisplay();
		var readSurface = EglGetCurrentSurface(EGL_READ);
		var drawSurface = EglGetCurrentSurface(EGL_DRAW);
		if (!EglMakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (!EglMakeCurrent(display, drawSurface, readSurface, glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EglMakeCurrent)} failed.");
				}
			}
		});
	}

	public static void Register() => ApiExtensibility.Register(typeof(INativeOpenGLWrapper), _ => new MacOSNativeOpenGLWrapper());

	private class MacOSAngleNativeContext : INativeContext
	{
		private readonly IntPtr _handle;

		public MacOSAngleNativeContext()
		{
			if (!NativeLibrary.TryLoad("libGLESv2.dylib", typeof(MacOSNativeOpenGLWrapper).Assembly, DllImportSearchPath.UserDirectories, out _handle) || _handle == IntPtr.Zero)
			{
				throw new PlatformNotSupportedException("Unable to load libGLESv2.dylib.");
			}
		}

		public bool TryGetProcAddress(string proc, out nint addr, int? slot = null) => NativeLibrary.TryGetExport(_handle, proc, out addr);

		public nint GetProcAddress(string proc, int? slot = null)
		{
			if (TryGetProcAddress(proc, out var address, slot))
			{
				return address;
			}

			throw new InvalidOperationException($"No function was found with the name '{proc}'.");
		}

		public void Dispose() => NativeLibrary.Free(_handle);
	}

	// Copyright (c) 2006-2019 Stefanos Apostolopoulos for the Open Toolkit project.
	// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
	// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
	// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
