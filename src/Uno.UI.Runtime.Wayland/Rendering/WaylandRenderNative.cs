#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Uno.UI.Runtime.Wayland.Rendering;

internal static partial class WaylandRenderNative
{
	[LibraryImport("libwayland-egl.so.1")]
	internal static partial IntPtr wl_egl_window_create(IntPtr surface, int width, int height);
	[LibraryImport("libwayland-egl.so.1")]
	internal static partial void wl_egl_window_resize(IntPtr window, int width, int height, int dx, int dy);
	[LibraryImport("libwayland-egl.so.1")]
	internal static partial void wl_egl_window_destroy(IntPtr window);
	[LibraryImport("libc.so.6", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial int memfd_create(string name, uint flags);
	[LibraryImport("libc.so.6")]
	internal static partial int ftruncate(int fd, long length);
	[LibraryImport("libc.so.6")]
	internal static partial IntPtr mmap(IntPtr address, nuint length, int protection, int flags, int fd, long offset);
	[LibraryImport("libc.so.6")]
	internal static partial int munmap(IntPtr address, nuint length);
	[LibraryImport("libc.so.6")]
	internal static partial int close(int fd);
	[LibraryImport("libEGL.so.1")]
	internal static partial IntPtr eglGetPlatformDisplay(uint platform, IntPtr display, IntPtr attributes);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglInitialize(IntPtr display, out int major, out int minor);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglBindAPI(uint api);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglChooseConfig(IntPtr display, int[] attributes, [Out, MarshalUsing(CountElementName = nameof(count))] IntPtr[]? configs, int count, out int matches);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglGetConfigAttrib(IntPtr display, IntPtr config, int attribute, out int value);
	[LibraryImport("libEGL.so.1")]
	internal static partial IntPtr eglCreateContext(IntPtr display, IntPtr config, IntPtr shared, int[] attributes);
	[LibraryImport("libEGL.so.1")]
	internal static partial IntPtr eglCreateWindowSurface(IntPtr display, IntPtr config, IntPtr window, int[] attributes);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglMakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglSwapBuffers(IntPtr display, IntPtr surface);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglSwapInterval(IntPtr display, int interval);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglGetError();
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglDestroySurface(IntPtr display, IntPtr surface);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglDestroyContext(IntPtr display, IntPtr context);
	[LibraryImport("libEGL.so.1")]
	internal static partial int eglTerminate(IntPtr display);
	[LibraryImport("libEGL.so.1", StringMarshalling = StringMarshalling.Utf8)]
	internal static partial IntPtr eglGetProcAddress(string name);
}
