using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11NativeOpenGLWrapper : INativeOpenGLWrapper
{
	private static readonly Lazy<IntPtr> _libGL = new(() =>
		NativeLibrary.TryLoad("libGL.so.1", typeof(X11NativeOpenGLWrapper).Assembly, DllImportSearchPath.UserDirectories, out var handle) ? handle : IntPtr.Zero);

	private IntPtr _display;
	private IntPtr _glContext;
	private IntPtr _pBuffer;

	public unsafe X11NativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		if (XamlRootMap.GetHostForRoot(xamlRoot) is not X11XamlRootHost xamlRootHost)
		{
			throw new InvalidOperationException($"The XamlRoot and its XamlRootHost must be initialized on the element before constructing an {nameof(X11NativeOpenGLWrapper)}.");
		}

		_display = xamlRootHost.RootX11Window.Display;

		using var lockDisposable = X11Helper.XLock(_display);

		var glxAttribs = new int[]{
			GlxConsts.GLX_DRAWABLE_TYPE   , GlxConsts.GLX_PBUFFER_BIT,
			GlxConsts.GLX_RED_SIZE        , 8,
			GlxConsts.GLX_GREEN_SIZE      , 8,
			GlxConsts.GLX_BLUE_SIZE       , 8,
			GlxConsts.GLX_ALPHA_SIZE      , 8,
			GlxConsts.GLX_DEPTH_SIZE      , 8,
			GlxConsts.GLX_STENCIL_SIZE    , 8,
			(int)X11Helper.None
		};

		var fbConfigs = GlxInterface.glXChooseFBConfig(_display, XLib.XDefaultScreen(_display), glxAttribs, out var count);
		if (fbConfigs == null || *fbConfigs == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.glXChooseFBConfig)} failed to retrieve GLX framebuffer configurations.");
		}
		using var fbConfigsDisposable = new DisposableStruct<IntPtr>(static aa => { _ = XLib.XFree(aa); }, (IntPtr)fbConfigs);

		IntPtr bestFbc = IntPtr.Zero;
		for (var c = 0; c < count; c++)
		{
			XVisualInfo* visual = GlxInterface.glXGetVisualFromFBConfig(_display, fbConfigs[c]);
			using var visualDisposable = new DisposableStruct<IntPtr>(static aa => { _ = XLib.XFree(aa); }, (IntPtr)visual);
			if (visual->depth == 32) // 24bit color + 8bit stencil as requested above
			{
				bestFbc = fbConfigs[c];
				break;
			}
		}

		if (bestFbc == IntPtr.Zero)
		{
			throw new InvalidOperationException("Could not find a suitable framebuffer config.\n");
		}

		_glContext = GlxInterface.glXCreateNewContext(_display, bestFbc, GlxConsts.GLX_RGBA_TYPE, IntPtr.Zero, /* True */ 1);
		if (_glContext == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.glXCreateNewContext)} failed.");
		}
		_pBuffer = GlxInterface.glXCreatePbuffer(_display, bestFbc, new[] { (int)X11Helper.None });
		if (_pBuffer == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.glXCreatePbuffer)} failed.");
		}
	}

	public void Dispose()
	{
		using var lockDisposable = X11Helper.XLock(_display);

		if (_display != IntPtr.Zero && _pBuffer != IntPtr.Zero)
		{
			GlxInterface.glXDestroyPbuffer(_display, _pBuffer);
		}
		if (_display != IntPtr.Zero && _glContext != IntPtr.Zero)
		{
			GlxInterface.glXDestroyContext(_display, _glContext);
		}

		_display = default;
		_glContext = default;
		_pBuffer = default;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = GlxInterface.glXGetCurrentContext();
		var drawable = GlxInterface.glXGetCurrentDrawable();
		GlxInterface.glXMakeCurrent(_display, _pBuffer, _glContext);
		return Disposable.Create(() => GlxInterface.glXMakeCurrent(_display, drawable, glContext));
	}

	// Non-throwing loader for the neutral IGLRenderTarget seam. dlsym libGL.so.1 first so genuinely-absent
	// entry points resolve to 0 (accurate availability) — bare glXGetProcAddress returns a non-null dispatch
	// trampoline for EVERY name, which fools a backend's capability probing into calling unsupported
	// functions (a hard crash on Mesa). glXGetProcAddress then serves extensions libGL doesn't export.
	internal static nint GetProcAddressStatic(string proc)
	{
		if (_libGL.Value != IntPtr.Zero && NativeLibrary.TryGetExport(_libGL.Value, proc, out var addr))
		{
			return addr;
		}

		return GlxInterface.glXGetProcAddress(proc);
	}

	public IntPtr GetProcAddress(string proc) => GetProcAddressStatic(proc);
	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		addr = GetProcAddressStatic(proc);
		return addr != IntPtr.Zero;
	}
}
