#nullable enable
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Uno.UI.Helpers;

internal class EglHelper
{
	private const string libEGL = "libEGL";

	public const int EGL_DEFAULT_DISPLAY = 0;
	public const int EGL_NO_CONTEXT = 0;
	public const int EGL_NO_SURFACE = 0;
	public const int EGL_ALPHA_SIZE = 0x3021;
	public const int EGL_BLUE_SIZE = 0x3022;
	public const int EGL_GREEN_SIZE = 0x3023;
	public const int EGL_RED_SIZE = 0x3024;
	public const int EGL_DEPTH_SIZE = 0x3025;
	public const int EGL_STENCIL_SIZE = 0x3026;
	public const int EGL_SAMPLES = 0x3031;
	public const int EGL_NONE = 0x3038;
	public const int EGL_CONTEXT_CLIENT_VERSION = 0x3098;
	public const int EGL_DRAW = 0x3059;
	public const int EGL_READ = 0x305A;
	public const int EGL_RENDERABLE_TYPE = 0x3040;
	public const int EGL_OPENGL_ES2_BIT = 0x04;
	public const int EGL_OPENGL_ES3_BIT = 0x40;

	public static bool IsAvailable()
	{
		if (NativeLibrary.TryLoad(libEGL, Assembly.GetExecutingAssembly(), DllImportSearchPath.SafeDirectories, out var handle))
		{
			NativeLibrary.Free(handle);
			return true;
		}
		else
		{
			return false;
		}
	}

	public static (IntPtr surface, IntPtr glContext, int major, int minor, int samples, int stencil) InitializeGles2Context(IntPtr eglDisplay, IntPtr? window = null)
	{
		if (eglDisplay == IntPtr.Zero)
		{
			throw new ArgumentException($"{nameof(eglDisplay)} is null");
		}

		if (!EglInitialize(eglDisplay, out var major, out var minor))
		{
			throw new InvalidOperationException($"{nameof(EglInitialize)} failed: {Enum.GetName(EglHelper.EglGetError())}");
		}

		int[] attribList =
		{
			EGL_RED_SIZE, 8,
			EGL_GREEN_SIZE, 8,
			EGL_BLUE_SIZE, 8,
			EGL_ALPHA_SIZE, 8,
			EGL_DEPTH_SIZE, 8,
			EGL_STENCIL_SIZE, 1,
			EGL_RENDERABLE_TYPE, EGL_OPENGL_ES2_BIT,
			EGL_NONE
		};

		var configs = new IntPtr[1];
		var success = EglChooseConfig(eglDisplay, attribList, configs, configs.Length, out var numConfig);

		if (!success || numConfig < 1)
		{
			throw new InvalidOperationException($"{nameof(EglChooseConfig)} failed: {Enum.GetName(EglHelper.EglGetError())}");
		}

		if (!EglGetConfigAttrib(eglDisplay, configs[0], EGL_SAMPLES, out var samples))
		{
			throw new InvalidOperationException($"{nameof(EglGetConfigAttrib)} failed to get {nameof(EGL_SAMPLES)}: {Enum.GetName(EglGetError())}");
		}
		if (!EglGetConfigAttrib(eglDisplay, configs[0], EGL_STENCIL_SIZE, out var stencil))
		{
			throw new InvalidOperationException($"{nameof(EglGetConfigAttrib)} failed to get {nameof(EGL_STENCIL_SIZE)}: {Enum.GetName(EglGetError())}");
		}

		var glContext = EglCreateContext(eglDisplay, configs[0], EGL_NO_CONTEXT, [EGL_CONTEXT_CLIENT_VERSION, 2, EGL_NONE]);
		if (glContext == IntPtr.Zero)
		{
			throw new InvalidOperationException($"EGL context creation failed: {Enum.GetName(EglHelper.EglGetError())}");
		}

		IntPtr surface;
		if (window is null)
		{
			surface = EglCreatePbufferSurface(eglDisplay, configs[0], [EGL_NONE]);
			if (surface == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(EglCreatePbufferSurface)} failed to get {nameof(EGL_SAMPLES)}: {Enum.GetName(EglGetError())}");
			}
		}
		else
		{
			surface = EglCreatePlatformWindowSurface(eglDisplay, configs[0], window.Value, [EGL_NONE]);
			if (surface == IntPtr.Zero)
			{
				throw new InvalidOperationException($"{nameof(EglCreatePlatformWindowSurface)} failed to get {nameof(EGL_SAMPLES)}: {Enum.GetName(EglGetError())}");
			}
		}
		return (surface, glContext, major, minor, samples, stencil);
	}

	public static unsafe string GetGlVersionString()
	{
		var glGetString = (delegate* unmanaged[Cdecl]<int, byte*>)EglGetProcAddress("glGetString");

		var glVersionBytePtr = glGetString(/* GL_VERSION */ 0x1F02);
		var glVersionString = Marshal.PtrToStringUTF8((IntPtr)glVersionBytePtr)!;
		return glVersionString;
	}

	[DllImport(libEGL, EntryPoint = "eglGetDisplay")]
	public static extern IntPtr EglGetDisplay(IntPtr display_id);

	[DllImport(libEGL, EntryPoint = "eglInitialize")]
	public static extern bool EglInitialize(IntPtr dpy, out int major, out int minor);

	[DllImport(libEGL, EntryPoint = "eglChooseConfig")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool EglChooseConfig(IntPtr dpy, int[] attrib_list, [In][Out] IntPtr[] configs, int config_size, out int num_config);

	[DllImport(libEGL, EntryPoint = "eglGetError")]
	public static extern ErrorCode EglGetError();

	[DllImport(libEGL, EntryPoint = "eglCreateContext")]
	public static extern IntPtr EglCreateContext(IntPtr dpy, IntPtr config, IntPtr share_context, int[] attrib_list);

	[DllImport(libEGL, EntryPoint = "eglCreatePbufferSurface")]
	public static extern IntPtr EglCreatePbufferSurface(IntPtr dpy, IntPtr config, int[] attrib_list);

	[DllImport(libEGL, EntryPoint = "eglCreatePlatformWindowSurface")]
	public static extern IntPtr EglCreatePlatformWindowSurface(IntPtr dpy, IntPtr config, IntPtr native_window, int[] attrib_list);

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

	[DllImport(libEGL, EntryPoint = "eglGetProcAddress")]
	public static extern IntPtr EglGetProcAddress(string procname);

	[DllImport(libEGL, EntryPoint = "eglGetPlatformDisplay")]
	public static extern IntPtr EglGetPlatformDisplay(int platform, IntPtr nativeDisplay, int[]? attrs);

	[DllImport(libEGL, EntryPoint = "eglGetPlatformDisplayEXT")]
	public static extern IntPtr EglGetPlatformDisplayEXT(int platform, IntPtr nativeDisplay, int[]? attrs);

	[DllImport(libEGL, EntryPoint = "eglMakeCurrent")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool EglMakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);

	[DllImport(libEGL, EntryPoint = "eglSwapBuffers")]
	public static extern bool EglSwapBuffers(IntPtr display, IntPtr surface);

	[DllImport(libEGL, EntryPoint = "eglGetConfigAttrib")]
	public static extern bool EglGetConfigAttrib(IntPtr display, IntPtr config, int attribute, out int value);

	public enum ErrorCode
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
