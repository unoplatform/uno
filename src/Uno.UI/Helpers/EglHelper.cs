using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Uno.UI.Helpers;

internal class EglHelper
{
	private const string libEGL = "libEGL.so.1";

	public const int EGL_DEFAULT_DISPLAY = 0;
	public const int EGL_NO_CONTEXT = 0;
	public const int EGL_NO_SURFACE = 0;
	public const int EGL_ALPHA_SIZE = 0x3021;
	public const int EGL_BLUE_SIZE = 0x3022;
	public const int EGL_GREEN_SIZE = 0x3023;
	public const int EGL_RED_SIZE = 0x3024;
	public const int EGL_DEPTH_SIZE = 0x3025;
	public const int EGL_STENCIL_SIZE = 0x3026;
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

	[DllImport(libEGL, EntryPoint = "eglMakeCurrent")]
	[return: MarshalAs(UnmanagedType.I1)]
	public static extern bool EglMakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);

	[DllImport(libEGL, EntryPoint = "eglSwapBuffers")]
	public static extern bool EglSwapBuffers(IntPtr display, IntPtr surface);

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
