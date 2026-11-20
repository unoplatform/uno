#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland;

// GLCanvasElement renders into its own framebuffer object, so an offscreen pbuffer is enough.
// Desktop OpenGL matches the X11 host's GLX context; OpenGL ES 3 is the fallback.
internal sealed partial class WaylandNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private const int EglNone = 0x3038;
	private const int EglSurfaceType = 0x3033;
	private const int EglPbufferBit = 0x0001;
	private const int EglRenderableType = 0x3040;
	private const int EglOpenGLBit = 0x0008;
	private const int EglOpenGLES3Bit = 0x0040;
	private const int EglRedSize = 0x3024;
	private const int EglGreenSize = 0x3023;
	private const int EglBlueSize = 0x3022;
	private const int EglAlphaSize = 0x3021;
	private const int EglDepthSize = 0x3025;
	private const int EglStencilSize = 0x3026;
	private const int EglWidth = 0x3057;
	private const int EglHeight = 0x3056;
	private const int EglContextClientVersion = 0x3098;
	private const int EglDraw = 0x3059;
	private const int EglRead = 0x305A;
	private const uint EglOpenGLApi = 0x30A2;
	private const uint EglOpenGLESApi = 0x30A0;

	private IntPtr _display;
	private IntPtr _context;
	private IntPtr _pbuffer;
	private uint _api;

	internal WaylandNativeOpenGLWrapper(XamlRoot xamlRoot, WaylandConnection connection)
	{
		if (XamlRootMap.GetHostForRoot(xamlRoot) is not WaylandXamlRootHost)
		{
			throw new InvalidOperationException($"The XamlRoot and its XamlRootHost must be initialized on the element before constructing an {nameof(WaylandNativeOpenGLWrapper)}.");
		}

		_display = WaylandEglDisplay.Acquire(connection.Display.NativeHandle);
		var previousApi = eglQueryAPI();
		try
		{
			if (!TryCreate(EglOpenGLApi, EglOpenGLBit, [EglNone]) && !TryCreate(EglOpenGLESApi, EglOpenGLES3Bit, [EglContextClientVersion, 3, EglNone]))
			{
				throw new InvalidOperationException("Unable to create an OpenGL or OpenGL ES 3 EGL context.");
			}
		}
		catch
		{
			Dispose();
			throw;
		}
		finally
		{
			_ = WaylandRenderNative.eglBindAPI(previousApi);
		}

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Created a {(_api == EglOpenGLApi ? "desktop OpenGL" : "OpenGL ES 3")} context for {nameof(WaylandNativeOpenGLWrapper)}.");
		}
	}

	private bool TryCreate(uint api, int renderableBit, int[] contextAttributes)
	{
		var configs = new IntPtr[1];
		if (WaylandRenderNative.eglBindAPI(api) == 0
			|| WaylandRenderNative.eglChooseConfig(_display,
				[EglSurfaceType, EglPbufferBit, EglRenderableType, renderableBit, EglRedSize, 8, EglGreenSize, 8, EglBlueSize, 8, EglAlphaSize, 8, EglDepthSize, 24, EglStencilSize, 8, EglNone],
				configs, configs.Length, out var count) == 0
			|| count < 1)
		{
			return false;
		}
		var config = configs[0];
		var context = WaylandRenderNative.eglCreateContext(_display, config, IntPtr.Zero, contextAttributes);
		if (context == IntPtr.Zero)
		{
			return false;
		}
		var pbuffer = eglCreatePbufferSurface(_display, config, [EglWidth, 1, EglHeight, 1, EglNone]);
		if (pbuffer == IntPtr.Zero)
		{
			_ = WaylandRenderNative.eglDestroyContext(_display, context);
			return false;
		}
		_context = context;
		_pbuffer = pbuffer;
		_api = api;
		return true;
	}

	public IDisposable MakeCurrent()
	{
		var api = eglQueryAPI();
		var display = eglGetCurrentDisplay();
		var context = eglGetCurrentContext();
		var draw = eglGetCurrentSurface(EglDraw);
		var read = eglGetCurrentSurface(EglRead);
		if (WaylandRenderNative.eglBindAPI(_api) == 0)
		{
			throw new InvalidOperationException("eglBindAPI failed.");
		}
		if (WaylandRenderNative.eglMakeCurrent(_display, _pbuffer, _pbuffer, _context) == 0)
		{
			_ = WaylandRenderNative.eglBindAPI(api);
			throw new InvalidOperationException("eglMakeCurrent failed.");
		}
		return Disposable.Create(() =>
		{
			// OpenGL and OpenGL ES share the thread's current context: release ours under its own API,
			// or bind the previous API before restoring its context.
			bool restored;
			if (context == IntPtr.Zero)
			{
				restored = WaylandRenderNative.eglMakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0;
				restored &= WaylandRenderNative.eglBindAPI(api) != 0;
			}
			else
			{
				restored = WaylandRenderNative.eglBindAPI(api) != 0 && WaylandRenderNative.eglMakeCurrent(display, draw, read, context) != 0;
			}
			if (!restored && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to restore the previous EGL context.");
			}
		});
	}

	public IntPtr GetProcAddress(string proc) => WaylandRenderNative.eglGetProcAddress(proc);

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		addr = WaylandRenderNative.eglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public void Dispose()
	{
		if (_display == IntPtr.Zero)
		{
			return;
		}
		if (_pbuffer != IntPtr.Zero)
		{
			_ = WaylandRenderNative.eglDestroySurface(_display, _pbuffer);
		}
		if (_context != IntPtr.Zero)
		{
			_ = WaylandRenderNative.eglDestroyContext(_display, _context);
		}
		WaylandEglDisplay.Release(_display);
		_display = default;
		_context = default;
		_pbuffer = default;
	}

	[LibraryImport("libEGL.so.1")]
	private static partial IntPtr eglCreatePbufferSurface(IntPtr display, IntPtr config, int[] attributes);
	[LibraryImport("libEGL.so.1")]
	private static partial uint eglQueryAPI();
	[LibraryImport("libEGL.so.1")]
	private static partial IntPtr eglGetCurrentDisplay();
	[LibraryImport("libEGL.so.1")]
	private static partial IntPtr eglGetCurrentContext();
	[LibraryImport("libEGL.so.1")]
	private static partial IntPtr eglGetCurrentSurface(int readDraw);
}
