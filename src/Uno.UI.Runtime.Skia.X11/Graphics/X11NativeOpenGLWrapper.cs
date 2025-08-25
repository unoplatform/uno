using System;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.Graphics;
using Uno.UI.Hosting;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11NativeOpenGLWrapper : INativeOpenGLWrapper
{
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

	public IntPtr GetProcAddress(string proc) => GlxInterface.glXGetProcAddress(proc);
	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		addr = GlxInterface.glXGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}
}
